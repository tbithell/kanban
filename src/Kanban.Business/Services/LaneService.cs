using System.Data;
using FluentValidation;
using Kanban.Business.Interfaces;
using Kanban.Business.Transforms;
using Kanban.Contracts;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain.Entities;
using Kanban.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Kanban.Business.Services;

public sealed class LaneService : ILaneService
{
    private readonly ILaneRepository _laneRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardMemberRepository _boardMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IDbConnection _dbConnection;
    private readonly IDbConnectionFactory _transactionFactory;
    private readonly ILogger<LaneService> _logger;

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

    public LaneService(
        ILaneRepository laneRepository,
        IBoardRepository boardRepository,
        IBoardMemberRepository boardMemberRepository,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IDbConnection dbConnection,
        IDbConnectionFactory transactionFactory,
        ILogger<LaneService> logger)
    {
        ArgumentNullException.ThrowIfNull(laneRepository);
        ArgumentNullException.ThrowIfNull(boardRepository);
        ArgumentNullException.ThrowIfNull(boardMemberRepository);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(authorizationService);
        ArgumentNullException.ThrowIfNull(dbConnection);
        ArgumentNullException.ThrowIfNull(transactionFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _laneRepository = laneRepository;
        _boardRepository = boardRepository;
        _boardMemberRepository = boardMemberRepository;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _dbConnection = dbConnection;
        _transactionFactory = transactionFactory;
        _logger = logger;
    }

    public async Task<LaneDto> CreateAsync(Guid boardId, CreateLaneRequest request)
    {
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        new InlineValidator<CreateLaneRequest>
            { v => v.RuleFor(x => x.Name).NotEmpty().MaximumLength(100) }
            .ValidateAndThrow(request);

        var userId = _currentUserService.UserId!.Value;
        _ = await _boardRepository.FindBoardForMemberAsync(boardId, userId)
            ?? throw new NotFoundException("board.not_found", "Board not found.");

        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal, new BoardContext(boardId), new BoardMembershipRequirement(BoardOperations.CreateLane));
        if (!auth.Succeeded)
            throw new ForbiddenException("lane.create_forbidden", "You do not have permission to create lanes on this board.");

        return await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                if (await _laneRepository.ExistsWithNameInBoardAsync(boardId, request.Name, tx))
                    throw new ConflictException("lane.name_conflict", "A lane with this name already exists on the board.");

                var count = await _laneRepository.CountInBoardAsync(boardId, tx);
                var position = count + 1;
                var lane = new Lane(Guid.NewGuid(), boardId, request.Name, position, 1, DateTimeOffset.UtcNow);
                await _laneRepository.InsertAsync(lane, tx);

                tx.Commit();
                _logger.LogInformation("Lane {LaneId} created on board {BoardId} by user {UserId}",
                    lane.Id, boardId, userId);
                return LaneTransforms.ToDto(lane, []);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }

    public async Task<LaneDto> RenameAsync(Guid boardId, Guid laneId, RenameLaneRequest request)
    {
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("laneId") }
            .ValidateAndThrow(laneId);
        new InlineValidator<RenameLaneRequest>
            { v => v.RuleFor(x => x.Name).NotEmpty().MaximumLength(100) }
            .ValidateAndThrow(request);

        var userId = _currentUserService.UserId!.Value;
        _ = await _boardRepository.FindBoardForMemberAsync(boardId, userId)
            ?? throw new NotFoundException("board.not_found", "Board not found.");

        var lane = await _laneRepository.FindByIdAsync(laneId)
            ?? throw new NotFoundException("lane.not_found", "Lane not found.");

        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal, new BoardContext(boardId), new BoardMembershipRequirement(BoardOperations.UpdateLane));
        if (!auth.Succeeded)
            throw new ForbiddenException("lane.update_forbidden", "You do not have permission to update lanes on this board.");

        return await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                if (await _laneRepository.ExistsWithNameInBoardAsync(boardId, request.Name, tx))
                    throw new ConflictException("lane.name_conflict", "A lane with this name already exists on the board.");

                lane.Rename(request.Name);
                await _laneRepository.UpdateNameAsync(laneId, request.Name, tx);

                tx.Commit();
                return LaneTransforms.ToDto(lane, []);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }

    public async Task MoveAsync(Guid boardId, Guid laneId, MoveLaneRequest request)
    {
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("laneId") }
            .ValidateAndThrow(laneId);

        var userId = _currentUserService.UserId!.Value;
        _ = await _boardRepository.FindBoardForMemberAsync(boardId, userId)
            ?? throw new NotFoundException("board.not_found", "Board not found.");

        var lane = await _laneRepository.FindByIdAsync(laneId)
            ?? throw new NotFoundException("lane.not_found", "Lane not found.");

        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal, new BoardContext(boardId), new BoardMembershipRequirement(BoardOperations.UpdateLane));
        if (!auth.Succeeded)
            throw new ForbiddenException("lane.update_forbidden", "You do not have permission to update lanes on this board.");

        var oldPosition = lane.Position;
        var newPosition = request.TargetPosition;

        await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                var rowsAffected = await _laneRepository.UpdatePositionAsync(laneId, 0, request.ExpectedVersion, tx);
                if (rowsAffected == 0)
                    throw new ConflictException("lane.version_conflict", "The lane was modified by another user. Please refresh and try again.");

                if (newPosition < oldPosition)
                    await _laneRepository.ShiftPositionsAsync(boardId, newPosition, oldPosition - 1, 1, tx);
                else if (newPosition > oldPosition)
                    await _laneRepository.ShiftPositionsAsync(boardId, oldPosition + 1, newPosition, -1, tx);

                await _laneRepository.SetPositionAsync(laneId, newPosition, tx);

                tx.Commit();
                _logger.LogInformation("Lane {LaneId} moved to position {Position} on board {BoardId} by user {UserId}",
                    laneId, newPosition, boardId, userId);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }

    public async Task DeleteAsync(Guid boardId, Guid laneId)
    {
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("laneId") }
            .ValidateAndThrow(laneId);

        var userId = _currentUserService.UserId!.Value;
        _ = await _boardRepository.FindBoardForMemberAsync(boardId, userId)
            ?? throw new NotFoundException("board.not_found", "Board not found.");

        var lane = await _laneRepository.FindByIdAsync(laneId)
            ?? throw new NotFoundException("lane.not_found", "Lane not found.");

        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal, new BoardContext(boardId), new BoardMembershipRequirement(BoardOperations.DeleteLane));
        if (!auth.Succeeded)
            throw new ForbiddenException("lane.delete_forbidden", "You do not have permission to delete lanes on this board.");

        var deletedPosition = lane.Position;

        await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                await _laneRepository.DeleteAsync(laneId, tx);
                await _laneRepository.ShiftPositionsAsync(boardId, deletedPosition + 1, int.MaxValue, -1, tx);

                tx.Commit();
                _logger.LogInformation("Lane {LaneId} deleted from board {BoardId} by user {UserId}",
                    laneId, boardId, userId);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }
}
