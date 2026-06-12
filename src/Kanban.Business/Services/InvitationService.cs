using System.Data;
using FluentValidation;
using Kanban.Business.Infrastructure;
using Kanban.Business.Interfaces;
using Kanban.Business.Transforms;
using Kanban.Contracts;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Domain.Events;
using Kanban.Domain.Exceptions;
using Kanban.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Kanban.Business.Services;

public sealed class InvitationService : IInvitationService
{
    private readonly IUserRepository _userRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IAuthEventRepository _authEventRepository;
    private readonly IBoardMemberRepository _boardMemberRepository;
    private readonly IDbConnection _dbConnection;
    private readonly IDbConnectionFactory _transactionFactory;
    private readonly ILogger<InvitationService> _logger;

    public InvitationService(
        IUserRepository userRepository,
        IInvitationRepository invitationRepository,
        IAuthEventRepository authEventRepository,
        IBoardMemberRepository boardMemberRepository,
        IDbConnection dbConnection,
        IDbConnectionFactory transactionFactory,
        ILogger<InvitationService> logger)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(invitationRepository);
        ArgumentNullException.ThrowIfNull(authEventRepository);
        ArgumentNullException.ThrowIfNull(boardMemberRepository);
        ArgumentNullException.ThrowIfNull(dbConnection);
        ArgumentNullException.ThrowIfNull(transactionFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _userRepository = userRepository;
        _invitationRepository = invitationRepository;
        _authEventRepository = authEventRepository;
        _boardMemberRepository = boardMemberRepository;
        _dbConnection = dbConnection;
        _transactionFactory = transactionFactory;
        _logger = logger;
    }

    public async Task<(IssueInviteResponse Response, bool IsNew)> IssueAsync(
        string email,
        Guid issuedByUserId,
        SystemRole callerRole,
        string frontendBaseUrl,
        Guid? boardId = null,
        BoardRole? boardRole = null,
        CancellationToken cancellationToken = default)
    {
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("email") }.ValidateAndThrow(email);
        new InlineValidator<Guid> { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("issuedByUserId") }.ValidateAndThrow(issuedByUserId);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("frontendBaseUrl") }.ValidateAndThrow(frontendBaseUrl);

        // System invitations require admin. Board invitations are pre-authorised by the caller
        // (BoardMembershipService checks board role before calling here).
        if (boardId is null && callerRole != SystemRole.Admin)
            throw new ForbiddenException("invite.forbidden", "Only admins can issue invitations.");

        return await SqliteRetryPolicy.Pipeline.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                var existing = await _invitationRepository.FindActiveByEmailAsync(email, tx);
                if (existing is not null)
                {
                    var refreshToken = InvitationToken.Generate();
                    var newExpiry = DateTimeOffset.UtcNow.AddDays(7);
                    await _invitationRepository.RefreshTokenAsync(
                        existing.Id, refreshToken.Hash, newExpiry, tx);
                    tx.Commit();
                    _logger.LogInformation(
                        "Active invitation refreshed by {IssuedByUserId}", issuedByUserId);
                    return (InvitationTransforms.ToResponse(existing.Id, refreshToken.Raw, newExpiry, frontendBaseUrl), false);
                }

                var registeredUser = await _userRepository.FindByEmailAsync(email, tx);
                if (registeredUser is not null)
                    throw new ConflictException(
                        "invite.already_registered", "This email address is already registered.");

                var token = InvitationToken.Generate();
                var issuedAt = DateTimeOffset.UtcNow;
                var invitation = new Invitation(
                    id: Guid.NewGuid(),
                    email: email,
                    issuedByUserId: issuedByUserId,
                    tokenHash: token.Hash,
                    issuedAt: issuedAt,
                    expiresAt: issuedAt.AddDays(7),
                    consumedAt: null,
                    consumedByUserId: null,
                    boardId: boardId,
                    boardRole: boardRole);

                await _invitationRepository.InsertAsync(invitation, tx);

                var authEvent = new AuthEvent(
                    Id: Guid.NewGuid(),
                    OccurredAt: issuedAt,
                    EventType: AuthEventType.InvitationIssued,
                    UserId: issuedByUserId,
                    Outcome: "success");
                await _authEventRepository.RecordAsync(authEvent, tx);

                tx.Commit();

                _logger.LogInformation(
                    "Invitation issued by {IssuedByUserId}", issuedByUserId);

                return (InvitationTransforms.ToResponse(invitation, token.Raw, frontendBaseUrl), true);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }, cancellationToken);
    }

    public async Task<(User User, Guid? BoardId)> AcceptAsync(
        string rawToken,
        string googleEmail,
        string googleSub,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("rawToken") }.ValidateAndThrow(rawToken);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("googleEmail") }.ValidateAndThrow(googleEmail);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("googleSub") }.ValidateAndThrow(googleSub);
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("displayName") }.ValidateAndThrow(displayName);

        return await SqliteRetryPolicy.Pipeline.ExecuteAsync(async ct =>
        {
            var tokenHash = InvitationToken.HashRaw(rawToken);
            var acceptedAt = DateTimeOffset.UtcNow;
            var newUserId = Guid.NewGuid();

            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                // Atomic gate: marks consumed_at — serialises concurrent accepts.
                // consumed_by_user_id is set separately below (after user is created) to avoid
                // FK constraint violations in Microsoft.Data.Sqlite 10+ which enforces them by
                // default, unlike previous versions.
                var consumed = await _invitationRepository.TryConsumeAsync(tokenHash, acceptedAt, tx);
                if (!consumed)
                    throw new NotFoundException(
                        "invite.invalid",
                        "This invitation is no longer valid. Please request a new one.");

                var invitation = await _invitationRepository.FindByTokenHashAsync(tokenHash, tx);
                if (invitation is null || !invitation.EmailMatches(googleEmail))
                    throw new BusinessRuleException(
                        "invite.email_mismatch",
                        "This invitation was issued to a different email address.");

                var existingUser = await _userRepository.FindByEmailAsync(invitation.Email, tx);
                if (existingUser is not null)
                    throw new ConflictException(
                        "invite.already_registered",
                        "This email address is already registered.");

                var user = new User(
                    id: newUserId,
                    email: invitation.Email,
                    displayName: displayName,
                    systemRole: SystemRole.Standard,
                    googleSub: googleSub,
                    registeredAt: acceptedAt,
                    lastSignInAt: null);

                await _userRepository.InsertAsync(user, tx);
                await _invitationRepository.RecordConsumerAsync(tokenHash, newUserId, tx);

                if (invitation.BoardId.HasValue && invitation.BoardRole.HasValue)
                {
                    var boardMember = new BoardMember(
                        id: Guid.NewGuid(),
                        boardId: invitation.BoardId.Value,
                        userId: newUserId,
                        role: invitation.BoardRole.Value,
                        invitedByUserId: invitation.IssuedByUserId,
                        joinedAt: acceptedAt);
                    await _boardMemberRepository.InsertAsync(boardMember, tx);
                }

                var authEvent = new AuthEvent(
                    Id: Guid.NewGuid(),
                    OccurredAt: acceptedAt,
                    EventType: AuthEventType.InvitationAccepted,
                    UserId: newUserId,
                    Outcome: "success");
                await _authEventRepository.RecordAsync(authEvent, tx);

                tx.Commit();

                _logger.LogInformation("Invitation accepted by user {UserId}", newUserId);
                return (user, invitation.BoardId);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }, cancellationToken);
    }
}
