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

public sealed class CardService : ICardService
{
    private readonly ICardRepository _cardRepository;
    private readonly ILaneRepository _laneRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardMemberRepository _boardMemberRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IDbConnection _dbConnection;
    private readonly IDbConnectionFactory _transactionFactory;
    private readonly ILogger<CardService> _logger;

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

    public CardService(
        ICardRepository cardRepository,
        ILaneRepository laneRepository,
        IBoardRepository boardRepository,
        IBoardMemberRepository boardMemberRepository,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IDbConnection dbConnection,
        IDbConnectionFactory transactionFactory,
        ILogger<CardService> logger)
    {
        ArgumentNullException.ThrowIfNull(cardRepository);
        ArgumentNullException.ThrowIfNull(laneRepository);
        ArgumentNullException.ThrowIfNull(boardRepository);
        ArgumentNullException.ThrowIfNull(boardMemberRepository);
        ArgumentNullException.ThrowIfNull(currentUserService);
        ArgumentNullException.ThrowIfNull(authorizationService);
        ArgumentNullException.ThrowIfNull(dbConnection);
        ArgumentNullException.ThrowIfNull(transactionFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _cardRepository = cardRepository;
        _laneRepository = laneRepository;
        _boardRepository = boardRepository;
        _boardMemberRepository = boardMemberRepository;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
        _dbConnection = dbConnection;
        _transactionFactory = transactionFactory;
        _logger = logger;
    }

    public async Task<CardDto> CreateAsync(Guid boardId, Guid laneId, CreateCardRequest request)
    {
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("laneId") }
            .ValidateAndThrow(laneId);
        new InlineValidator<CreateCardRequest>
            { v => v.RuleFor(x => x.Title).NotEmpty().MaximumLength(200) }
            .ValidateAndThrow(request);

        var userId = _currentUserService.UserId!.Value;
        _ = await _boardRepository.FindBoardForMemberAsync(boardId, userId)
            ?? throw new NotFoundException("board.not_found", "Board not found.");

        var lane = await _laneRepository.FindByIdAsync(laneId)
            ?? throw new NotFoundException("lane.not_found", "Lane not found.");
        if (lane.BoardId != boardId)
            throw new NotFoundException("lane.not_found", "Lane not found.");

        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal,
            new BoardContext(boardId),
            new BoardMembershipRequirement(BoardOperations.CreateCard));
        if (!auth.Succeeded)
            throw new ForbiddenException("card.create_forbidden", "You do not have permission to create cards on this board.");

        return await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                var count = await _cardRepository.CountInLaneAsync(laneId, tx);
                var position = count + 1;
                var now = DateTimeOffset.UtcNow;
                var card = new Card(Guid.NewGuid(), laneId, boardId, request.Title,
                    request.Description, request.DueDate, position, 1, now, now);
                await _cardRepository.InsertAsync(card, tx);

                tx.Commit();
                _logger.LogInformation("Card {CardId} created in lane {LaneId} on board {BoardId} by user {UserId}",
                    card.Id, laneId, boardId, userId);
                return CardTransforms.ToDto(card);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }

    public async Task<CardDto> UpdateAsync(Guid boardId, Guid cardId, UpdateCardRequest request)
    {
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("cardId") }
            .ValidateAndThrow(cardId);

        var userId = _currentUserService.UserId!.Value;
        _ = await _boardRepository.FindBoardForMemberAsync(boardId, userId)
            ?? throw new NotFoundException("board.not_found", "Board not found.");

        var card = await _cardRepository.FindByIdAsync(cardId)
            ?? throw new NotFoundException("card.not_found", "Card not found.");
        if (card.BoardId != boardId)
            throw new NotFoundException("card.not_found", "Card not found.");

        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal,
            new BoardContext(boardId),
            new BoardMembershipRequirement(BoardOperations.UpdateCard));
        if (!auth.Succeeded)
            throw new ForbiddenException("card.update_forbidden", "You do not have permission to update cards on this board.");

        var newTitle = request.Title ?? card.Title;
        var newDescription = request.Description;
        var newDueDate = request.ClearDueDate ? null : (request.DueDate ?? card.DueDate);

        return await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                card.Update(newTitle, newDescription, newDueDate, DateTimeOffset.UtcNow);
                await _cardRepository.UpdateAsync(card, tx);

                tx.Commit();
                return CardTransforms.ToDto(card);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }

    public async Task<CardDto> MoveAsync(Guid boardId, Guid cardId, MoveCardRequest request)
    {
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("cardId") }
            .ValidateAndThrow(cardId);

        var userId = _currentUserService.UserId!.Value;
        _ = await _boardRepository.FindBoardForMemberAsync(boardId, userId)
            ?? throw new NotFoundException("board.not_found", "Board not found.");

        var preCheck = await _cardRepository.FindByIdAsync(cardId)
            ?? throw new NotFoundException("card.not_found", "Card not found.");
        if (preCheck.BoardId != boardId)
            throw new NotFoundException("card.not_found", "Card not found.");

        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal,
            new BoardContext(boardId),
            new BoardMembershipRequirement(BoardOperations.UpdateCard));
        if (!auth.Succeeded)
            throw new ForbiddenException("card.move_forbidden", "You do not have permission to move cards on this board.");

        var targetLaneId = request.TargetLaneId;
        var newPosition = request.TargetPosition;

        return await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                // Re-read current card state per attempt so retry shifts use fresh positions.
                var current = await _cardRepository.FindByIdAsync(cardId, tx)
                    ?? throw new NotFoundException("card.not_found", "Card not found.");
                var currentSourceLaneId = current.LaneId;
                var currentOldPosition = current.Position;
                var isCrossLane = currentSourceLaneId != targetLaneId;

                await _cardRepository.ParkCardAsync(cardId, tx);

                if (isCrossLane)
                    await _cardRepository.ShiftPositionsInLaneAsync(targetLaneId, newPosition, int.MaxValue, 1, tx);
                else if (newPosition < currentOldPosition)
                    await _cardRepository.ShiftPositionsInLaneAsync(currentSourceLaneId, newPosition, currentOldPosition - 1, 1, tx);
                else if (newPosition > currentOldPosition)
                    await _cardRepository.ShiftPositionsInLaneAsync(currentSourceLaneId, currentOldPosition + 1, newPosition, -1, tx);

                var rowsAffected = await _cardRepository.UpdatePositionAsync(
                    cardId, targetLaneId, newPosition, request.ExpectedVersion, tx);
                if (rowsAffected == 0)
                    throw new ConflictException("card.version_conflict",
                        "The card was modified by another user. Please refresh and try again.");

                if (isCrossLane)
                    await _cardRepository.ShiftPositionsInLaneAsync(currentSourceLaneId, currentOldPosition + 1, int.MaxValue, -1, tx);

                var updated = await _cardRepository.FindByIdAsync(cardId, tx)
                    ?? throw new NotFoundException("card.not_found", "Card not found after move.");

                tx.Commit();
                _logger.LogInformation("Card {CardId} moved to lane {LaneId} position {Position} on board {BoardId} by user {UserId}",
                    cardId, targetLaneId, newPosition, boardId, userId);
                return CardTransforms.ToDto(updated);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }

    public async Task DeleteAsync(Guid boardId, Guid cardId)
    {
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("boardId") }
            .ValidateAndThrow(boardId);
        new InlineValidator<Guid>
            { v => v.RuleFor(x => x).NotEqual(Guid.Empty).WithName("cardId") }
            .ValidateAndThrow(cardId);

        var userId = _currentUserService.UserId!.Value;
        _ = await _boardRepository.FindBoardForMemberAsync(boardId, userId)
            ?? throw new NotFoundException("board.not_found", "Board not found.");

        var card = await _cardRepository.FindByIdAsync(cardId)
            ?? throw new NotFoundException("card.not_found", "Card not found.");
        if (card.BoardId != boardId)
            throw new NotFoundException("card.not_found", "Card not found.");

        var auth = await _authorizationService.AuthorizeAsync(
            _currentUserService.Principal,
            new BoardContext(boardId),
            new BoardMembershipRequirement(BoardOperations.DeleteCard));
        if (!auth.Succeeded)
            throw new ForbiddenException("card.delete_forbidden", "You do not have permission to delete cards on this board.");

        await RetryPolicy.ExecuteAsync(async ct =>
        {
            using var tx = _transactionFactory.BeginDeferredTransaction(_dbConnection);
            try
            {
                var current = await _cardRepository.FindByIdAsync(cardId, tx)
                    ?? throw new NotFoundException("card.not_found", "Card not found.");
                await _cardRepository.DeleteAsync(cardId, tx);
                await _cardRepository.ShiftPositionsInLaneAsync(current.LaneId, current.Position + 1, int.MaxValue, -1, tx);

                tx.Commit();
                _logger.LogInformation("Card {CardId} deleted from lane {LaneId} on board {BoardId} by user {UserId}",
                    cardId, current.LaneId, boardId, userId);
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        });
    }
}
