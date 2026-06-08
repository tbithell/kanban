using System.Data;
using FluentValidation;
using Kanban.Business.Interfaces;
using Kanban.Business.Transforms;
using Kanban.Contracts;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Kanban.Business.Services;

public sealed class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardMemberRepository _boardMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IDbConnection _dbConnection;
    private readonly IDbConnectionFactory _transactionFactory;
    private readonly ILogger<BoardService> _logger;

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

    public BoardService(
        IBoardRepository boardRepository,
        IBoardMemberRepository boardMemberRepository,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IDbConnection dbConnection,
        IDbConnectionFactory transactionFactory,
        ILogger<BoardService> logger)
    {
        ArgumentNullException.ThrowIfNull(boardRepository);
        ArgumentNullException.ThrowIfNull(boardMemberRepository);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(authorizationService);
        ArgumentNullException.ThrowIfNull(dbConnection);
        ArgumentNullException.ThrowIfNull(transactionFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _boardRepository = boardRepository;
        _boardMemberRepository = boardMemberRepository;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _dbConnection = dbConnection;
        _transactionFactory = transactionFactory;
        _logger = logger;
    }

    public async Task<BoardDetailDto> CreateAsync(CreateBoardRequest request)
    {
        new InlineValidator<CreateBoardRequest>
            { v => v.RuleFor(x => x.Name).NotEmpty().MaximumLength(200) }
            .ValidateAndThrow(request);

        var adminAuth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal, null, "Admin");
        if (!adminAuth.Succeeded)
            throw new ForbiddenException("board.create_forbidden", "Only admins can create boards.");

        var userId = _currentUserService.UserId!.Value;

        return await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                if (await _boardRepository.ExistsWithNameAsync(request.Name, tx))
                    throw new ConflictException("board.name_conflict", "A board with this name already exists.");

                var board = new Board(Guid.NewGuid(), request.Name, userId, DateTimeOffset.UtcNow);
                await _boardRepository.InsertAsync(board, tx);

                var member = new BoardMember(
                    Guid.NewGuid(), board.Id, userId, BoardRole.Owner, null, DateTimeOffset.UtcNow);
                await _boardMemberRepository.InsertAsync(member, tx);

                tx.Commit();
                _logger.LogInformation("Board {BoardId} created by user {UserId}", board.Id, userId);
                return BoardTransforms.ToDetailDto(board, BoardRole.Owner, []);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }

    public async Task<IReadOnlyList<BoardSummaryDto>> ListForCallerAsync()
    {
        var userId = _currentUserService.UserId!.Value;
        var boards = await _boardRepository.FindBoardsForUserAsync(userId);

        var summaries = new List<BoardSummaryDto>(boards.Count);
        foreach (var board in boards)
        {
            var role = await _boardMemberRepository.FindRoleAsync(board.Id, userId)
                ?? BoardRole.Viewer;
            summaries.Add(BoardTransforms.ToSummaryDto(board, role, 0, 0));
        }
        return summaries;
    }

    public async Task<BoardDetailDto> GetAsync(Guid boardId)
    {
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);

        var userId = _currentUserService.UserId!.Value;
        var board = await _boardRepository.FindBoardForMemberAsync(boardId, userId)
            ?? throw new NotFoundException("board.not_found", "Board not found.");

        var role = await _boardMemberRepository.FindRoleAsync(boardId, userId)
            ?? BoardRole.Viewer;

        return BoardTransforms.ToDetailDto(board, role, []);
    }

    public async Task DeleteAsync(Guid boardId)
    {
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);

        var userId = _currentUserService.UserId!.Value;
        _ = await _boardRepository.FindBoardForMemberAsync(boardId, userId)
            ?? throw new NotFoundException("board.not_found", "Board not found.");

        var deleteAuth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal, new BoardContext(boardId),
            new BoardMembershipRequirement(BoardOperations.DeleteBoard));
        if (!deleteAuth.Succeeded)
            throw new ForbiddenException("board.delete_forbidden", "You do not have permission to delete this board.");

        await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                await _boardRepository.DeleteAsync(boardId, tx);
                tx.Commit();
                _logger.LogInformation("Board {BoardId} deleted by user {UserId}", boardId, userId);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }
}
