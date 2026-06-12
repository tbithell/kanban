using System.Data;
using FluentValidation;
using Kanban.Business.Interfaces;
using Kanban.Business.Transforms;
using Kanban.Contracts;
using Kanban.DataAccess;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Kanban.Business.Services;

public sealed class BoardMembershipService : IBoardMembershipService
{
    private readonly IBoardMemberRepository _boardMemberRepository;
    private readonly IInvitationService _invitationService;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IDbConnection _dbConnection;
    private readonly IDbConnectionFactory _transactionFactory;
    private readonly ILogger<BoardMembershipService> _logger;

    private static readonly ResiliencePipeline RetryPolicy =
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                    ex.Message.Contains("SQLITE_BUSY", StringComparison.OrdinalIgnoreCase) ||
                    ex.Message.Contains("database is locked", StringComparison.OrdinalIgnoreCase)),
            })
            .Build();

    public BoardMembershipService(
        IBoardMemberRepository boardMemberRepository,
        IInvitationService invitationService,
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IDbConnection dbConnection,
        IDbConnectionFactory transactionFactory,
        ILogger<BoardMembershipService> logger)
    {
        ArgumentNullException.ThrowIfNull(boardMemberRepository);
        ArgumentNullException.ThrowIfNull(invitationService);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(authorizationService);
        ArgumentNullException.ThrowIfNull(dbConnection);
        ArgumentNullException.ThrowIfNull(transactionFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _boardMemberRepository = boardMemberRepository;
        _invitationService = invitationService;
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _dbConnection = dbConnection;
        _transactionFactory = transactionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BoardMemberDto>> ListMembersAsync(Guid boardId)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);

        var callerId = _currentUserService.UserId!.Value;
        var callerRole = await _boardMemberRepository.FindRoleAsync(boardId, callerId) ?? throw new NotFoundException("board.not_found", "Board not found.");
        var members = await _boardMemberRepository.FindAllForBoardAsync(boardId);

        var userIds = members.Select(m => m.UserId).ToList();
        var users = await _userRepository.FindByIdsAsync(userIds);
        var userMap = users.ToDictionary(u => u.Id, u => u.DisplayName);

        return members
            .OrderBy(m => m.JoinedAt)
            .Select(m => BoardMemberTransforms.ToDto(m, userMap.GetValueOrDefault(m.UserId, m.UserId.ToString())))
            .ToList();
    }

    public async Task InviteAsync(Guid boardId, InviteBoardMemberRequest request, string frontendBaseUrl)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        ArgumentNullException.ThrowIfNull(request);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("frontendBaseUrl") }
            .ValidateAndThrow(frontendBaseUrl);

        var callerId = _currentUserService.UserId!.Value;
        var callerRole = await _boardMemberRepository.FindRoleAsync(boardId, callerId) ?? throw new NotFoundException("board.not_found", "Board not found.");
        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal,
            new BoardContext(boardId),
            new BoardMembershipRequirement(BoardOperations.ManageMembers));
        if (!auth.Succeeded)
            throw new ForbiddenException("member.forbidden", "Only board owners can invite members.");

        var callerSystemRole = _currentUserService.SystemRole == "admin"
            ? SystemRole.Admin
            : SystemRole.Standard;

        var existingUser = await _userRepository.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            var existingRole = await _boardMemberRepository.FindRoleAsync(boardId, existingUser.Id);
            if (existingRole is not null)
                throw new ConflictException("member.already_member", "This user is already a member of the board.");

            var boardRole = ToDomainRole(request.Role);
            await RetryPolicy.ExecuteAsync(async _ =>
            {
                using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
                try
                {
                    var member = new BoardMember(
                        id: Guid.NewGuid(),
                        boardId: boardId,
                        userId: existingUser.Id,
                        role: boardRole,
                        invitedByUserId: callerId,
                        joinedAt: DateTimeOffset.UtcNow);
                    await _boardMemberRepository.InsertAsync(member, tx);
                    tx.Commit();
                    _logger.LogInformation(
                        "Existing user {UserId} added to board {BoardId} with role {Role} by {CallerId}",
                        existingUser.Id, boardId, boardRole, callerId);
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }, CancellationToken.None);
            return;
        }

        var (inviteResponse, _) = await _invitationService.IssueAsync(
            request.Email,
            callerId,
            callerSystemRole,
            frontendBaseUrl,
            boardId: boardId,
            boardRole: ToDomainRole(request.Role));

        _logger.LogInformation(
            "Board invite {InvitationId} issued to board {BoardId} by {CallerId}",
            inviteResponse.InvitationId, boardId, callerId);
    }

    public async Task<BoardMemberDto> ChangeRoleAsync(Guid boardId, Guid userId, ChangeMemberRoleRequest request)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("userId") }
            .ValidateAndThrow(userId);
        ArgumentNullException.ThrowIfNull(request);

        var callerId = _currentUserService.UserId!.Value;
        var callerRole = await _boardMemberRepository.FindRoleAsync(boardId, callerId) ?? throw new NotFoundException("board.not_found", "Board not found.");
        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal,
            new BoardContext(boardId),
            new BoardMembershipRequirement(BoardOperations.ManageMembers));
        if (!auth.Succeeded)
            throw new ForbiddenException("member.forbidden", "Only board owners can change member roles.");

        var preflightTargetRole = await _boardMemberRepository.FindRoleAsync(boardId, userId) ?? throw new NotFoundException("member.not_found", "Target user is not a member of this board.");
        var newRole = ToDomainRole(request.Role);

        return await RetryPolicy.ExecuteAsync(async _ =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                var currentTargetRole = await _boardMemberRepository.FindRoleAsync(boardId, userId, tx);
                if (newRole != BoardRole.Owner && currentTargetRole == BoardRole.Owner)
                {
                    var ownerCount = await _boardMemberRepository.CountOwnersAsync(boardId, tx);
                    if (ownerCount <= 1)
                        throw new BusinessRuleException("member.last_owner", "Cannot remove the last owner from the board.");
                }

                await _boardMemberRepository.UpdateRoleAsync(boardId, userId, newRole, tx);

                var members = await _boardMemberRepository.FindAllForBoardAsync(boardId, tx);
                var updated = members.FirstOrDefault(m => m.UserId == userId)
                    ?? throw new NotFoundException("member.not_found", "Target user is not a member of this board.");
                var user = await _userRepository.FindByIdAsync(userId, tx);
                var displayName = user?.DisplayName ?? userId.ToString();

                tx.Commit();
                _logger.LogInformation(
                    "Member {UserId} role changed to {Role} on board {BoardId} by {CallerId}",
                    userId, newRole, boardId, callerId);
                return BoardMemberTransforms.ToDto(updated, displayName);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }, CancellationToken.None);
    }

    public async Task RemoveMemberAsync(Guid boardId, Guid userId)
    {
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("userId") }
            .ValidateAndThrow(userId);

        var callerId = _currentUserService.UserId!.Value;
        var callerRole = await _boardMemberRepository.FindRoleAsync(boardId, callerId) ?? throw new NotFoundException("board.not_found", "Board not found.");
        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal,
            new BoardContext(boardId),
            new BoardMembershipRequirement(BoardOperations.ManageMembers));
        if (!auth.Succeeded)
            throw new ForbiddenException("member.forbidden", "Only board owners can remove members.");

        var preflightTargetRole = await _boardMemberRepository.FindRoleAsync(boardId, userId) ?? throw new NotFoundException("member.not_found", "Target user is not a member of this board.");
        await RetryPolicy.ExecuteAsync(async _ =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                var currentTargetRole = await _boardMemberRepository.FindRoleAsync(boardId, userId, tx);
                if (currentTargetRole == BoardRole.Owner)
                {
                    var ownerCount = await _boardMemberRepository.CountOwnersAsync(boardId, tx);
                    if (ownerCount <= 1)
                        throw new BusinessRuleException("member.last_owner", "Cannot remove the last owner from the board.");
                }

                await _boardMemberRepository.DeleteAsync(boardId, userId, tx);
                tx.Commit();
                _logger.LogInformation(
                    "Member {UserId} removed from board {BoardId} by {CallerId}", userId, boardId, callerId);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }, CancellationToken.None);
    }

    private static BoardRole ToDomainRole(BoardRoleDto dto) => dto switch
    {
        BoardRoleDto.Owner  => BoardRole.Owner,
        BoardRoleDto.Member => BoardRole.Member,
        BoardRoleDto.Viewer => BoardRole.Viewer,
        _                   => throw new ArgumentOutOfRangeException(nameof(dto)),
    };
}
