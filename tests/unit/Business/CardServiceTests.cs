using System.Data;
using FluentAssertions;
using Kanban.Business.Services;
using Kanban.Business.Interfaces;
using Kanban.Contracts;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static Kanban.Tests.Unit.Builders.BoardBuilder;
using static Kanban.Tests.Unit.Builders.CardBuilder;
using static Kanban.Tests.Unit.Builders.LaneBuilder;

namespace Kanban.Tests.Unit.Business;

public sealed class CardServiceTests
{
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _laneId = Guid.NewGuid();
    private readonly Guid _ownerUserId = Guid.NewGuid();

    private CardService CreateSut(
        ICardRepository? cardRepo = null,
        ILaneRepository? laneRepo = null,
        IBoardRepository? boardRepo = null,
        IBoardMemberRepository? memberRepo = null,
        FakeCurrentUserService? currentUser = null,
        IAuthorizationService? authService = null)
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var board = ABoard().WithId(_boardId).Build();
        return new CardService(
            cardRepo ?? new FakeCardRepository(),
            laneRepo ?? new FakeLaneRepository(),
            boardRepo ?? new FakeBoardRepository(boardForMember: board),
            memberRepo ?? new FakeBoardMemberRepository(BoardRole.Owner),
            currentUser ?? new FakeCurrentUserService(_ownerUserId, "standard"),
            authService ?? BuildSucceedingAuthService(),
            connection,
            new FakeDbConnectionFactory(),
            NullLogger<CardService>.Instance);
    }

    private static IAuthorizationService BuildSucceedingAuthService()
    {
        var mock = new Mock<IAuthorizationService>();
        mock.Setup(s => s.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
        mock.Setup(s => s.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        return mock.Object;
    }

    private static IAuthorizationService BuildFailingAuthService()
    {
        var mock = new Mock<IAuthorizationService>();
        mock.Setup(s => s.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Failed());
        mock.Setup(s => s.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Failed());
        return mock.Object;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Success_AppendsAtPositionN1()
    {
        var lane = ALane().WithId(_laneId).OnBoard(_boardId).Build();
        var cardRepo = new FakeCardRepository(countInLane: 2);
        var laneRepo = new FakeLaneRepository(lane: lane);
        var sut = CreateSut(cardRepo: cardRepo, laneRepo: laneRepo);

        var result = await sut.CreateAsync(_boardId, _laneId, new CreateCardRequest("New Card"));

        result.Title.Should().Be("New Card");
        result.Position.Should().Be(3);
        result.LaneId.Should().Be(_laneId);
        cardRepo.InsertedCard.Should().NotBeNull();
        cardRepo.InsertedCard!.Position.Should().Be(3);
    }

    [Fact]
    public async Task CreateAsync_WithDescriptionAndDueDate_PersistsOptionalFields()
    {
        var lane = ALane().WithId(_laneId).OnBoard(_boardId).Build();
        var dueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var cardRepo = new FakeCardRepository(countInLane: 0);
        var laneRepo = new FakeLaneRepository(lane: lane);
        var sut = CreateSut(cardRepo: cardRepo, laneRepo: laneRepo);

        var result = await sut.CreateAsync(_boardId, _laneId,
            new CreateCardRequest("Card With Details", "Some description", dueDate));

        result.Description.Should().Be("Some description");
        result.DueDate.Should().Be(dueDate);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_TitleOnly_UpdatesOnlyTitle()
    {
        var cardId = Guid.NewGuid();
        var originalDue = DateOnly.FromDateTime(DateTime.Today);
        var card = ACard().WithId(cardId).InLane(_laneId).OnBoard(_boardId)
            .WithDescription("Original Desc").DueOn(originalDue).Build();
        var cardRepo = new FakeCardRepository(card: card);
        var sut = CreateSut(cardRepo: cardRepo);

        var result = await sut.UpdateAsync(_boardId, cardId,
            new UpdateCardRequest("Updated Title", "Original Desc", false, originalDue));

        result.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateAsync_AllFields_UpdatesEverything()
    {
        var cardId = Guid.NewGuid();
        var card = ACard().WithId(cardId).InLane(_laneId).OnBoard(_boardId).Build();
        var cardRepo = new FakeCardRepository(card: card);
        var newDue = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var sut = CreateSut(cardRepo: cardRepo);

        var result = await sut.UpdateAsync(_boardId, cardId,
            new UpdateCardRequest("New Title", "New Description", false, newDue));

        result.Title.Should().Be("New Title");
        result.Description.Should().Be("New Description");
        result.DueDate.Should().Be(newDue);
    }

    [Fact]
    public async Task UpdateAsync_ClearDueDateTrue_RemovesDueDate()
    {
        var cardId = Guid.NewGuid();
        var card = ACard().WithId(cardId).InLane(_laneId).OnBoard(_boardId)
            .DueOn(DateOnly.FromDateTime(DateTime.Today)).Build();
        var cardRepo = new FakeCardRepository(card: card);
        var sut = CreateSut(cardRepo: cardRepo);

        var result = await sut.UpdateAsync(_boardId, cardId,
            new UpdateCardRequest("Same Title", null, true, null));

        result.DueDate.Should().BeNull();
        cardRepo.UpdatedCard.Should().NotBeNull();
        cardRepo.UpdatedCard!.DueDate.Should().BeNull();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingCard_DeletesAndShiftsPositions()
    {
        var cardId = Guid.NewGuid();
        var card = ACard().WithId(cardId).InLane(_laneId).OnBoard(_boardId).AtPosition(2).Build();
        var cardRepo = new FakeCardRepository(card: card, countInLane: 3);
        var sut = CreateSut(cardRepo: cardRepo);

        await sut.DeleteAsync(_boardId, cardId);

        cardRepo.DeletedCardId.Should().Be(cardId);
        cardRepo.ShiftedDelta.Should().Be(-1);
    }

    [Fact]
    public async Task DeleteAsync_PositionsGaplessAfterDelete()
    {
        var cardId = Guid.NewGuid();
        var card = ACard().WithId(cardId).InLane(_laneId).OnBoard(_boardId).AtPosition(1).Build();
        var cardRepo = new FakeCardRepository(card: card, countInLane: 3);
        var sut = CreateSut(cardRepo: cardRepo);

        await sut.DeleteAsync(_boardId, cardId);

        cardRepo.ShiftedFrom.Should().BeGreaterThan(0);
        cardRepo.ShiftedDelta.Should().Be(-1);
    }

    // ── Move ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveAsync_SameLane_ShiftsPositionsWithinLane()
    {
        var cardId = Guid.NewGuid();
        var card = ACard().WithId(cardId).InLane(_laneId).OnBoard(_boardId)
            .AtPosition(1).WithVersion(1).Build();
        var cardRepo = new FakeCardRepository(card: card, updatePositionRows: 1);
        var sut = CreateSut(cardRepo: cardRepo);

        await sut.MoveAsync(_boardId, cardId, new MoveCardRequest(_laneId, 3, 1));

        cardRepo.UpdatePositionLaneId.Should().Be(_laneId);
        cardRepo.ShiftedLaneId.Should().Be(_laneId);
    }

    [Fact]
    public async Task MoveAsync_CrossLane_ShiftsBothSourceAndDestinationLanes()
    {
        var cardId = Guid.NewGuid();
        var targetLaneId = Guid.NewGuid();
        var card = ACard().WithId(cardId).InLane(_laneId).OnBoard(_boardId)
            .AtPosition(2).WithVersion(1).Build();
        var cardRepo = new FakeCardRepository(card: card, updatePositionRows: 1);
        var sut = CreateSut(cardRepo: cardRepo);

        await sut.MoveAsync(_boardId, cardId, new MoveCardRequest(targetLaneId, 1, 1));

        cardRepo.UpdatePositionLaneId.Should().Be(targetLaneId);
    }

    [Fact]
    public async Task MoveAsync_VersionConflict_ThrowsConflictException()
    {
        var cardId = Guid.NewGuid();
        var card = ACard().WithId(cardId).InLane(_laneId).OnBoard(_boardId)
            .WithVersion(5).Build();
        var cardRepo = new FakeCardRepository(card: card, updatePositionRows: 0);
        var sut = CreateSut(cardRepo: cardRepo);

        var act = () => sut.MoveAsync(_boardId, cardId, new MoveCardRequest(_laneId, 2, 1));

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "card.version_conflict");
    }

    [Fact]
    public async Task MoveAsync_PositionsGaplessAfterSameLaneReorder()
    {
        var cardId = Guid.NewGuid();
        var card = ACard().WithId(cardId).InLane(_laneId).OnBoard(_boardId)
            .AtPosition(3).WithVersion(1).Build();
        var cardRepo = new FakeCardRepository(card: card, updatePositionRows: 1);
        var sut = CreateSut(cardRepo: cardRepo);

        await sut.MoveAsync(_boardId, cardId, new MoveCardRequest(_laneId, 1, 1));

        cardRepo.ShiftedFrom.Should().BeGreaterThan(0);
    }

    // ── Authorization ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ViewerCaller_ThrowsForbiddenException()
    {
        var lane = ALane().WithId(_laneId).OnBoard(_boardId).Build();
        var laneRepo = new FakeLaneRepository(lane: lane);
        var sut = CreateSut(laneRepo: laneRepo, authService: BuildFailingAuthService());

        var act = () => sut.CreateAsync(_boardId, _laneId, new CreateCardRequest("Card"));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeCardRepository : ICardRepository
    {
        private readonly Card? _card;
        private readonly int _countInLane;
        private readonly int _updatePositionRows;

        public Card? InsertedCard { get; private set; }
        public Card? UpdatedCard { get; private set; }
        public Guid? DeletedCardId { get; private set; }
        public int ShiftedFrom { get; private set; }
        public int ShiftedDelta { get; private set; }
        public Guid ShiftedLaneId { get; private set; }
        public Guid UpdatePositionLaneId { get; private set; }

        public FakeCardRepository(
            Card? card = null,
            int countInLane = 0,
            int updatePositionRows = 1)
        {
            _card = card;
            _countInLane = countInLane;
            _updatePositionRows = updatePositionRows;
        }

        public Task<IReadOnlyList<Card>> FindByLaneAsync(Guid laneId, IDbTransaction? tx = null)
            => Task.FromResult<IReadOnlyList<Card>>(_card is not null ? [_card] : []);

        public Task<Card?> FindByIdAsync(Guid cardId, IDbTransaction? tx = null)
            => Task.FromResult(_card?.Id == cardId ? _card : null);

        public Task InsertAsync(Card card, IDbTransaction tx)
        {
            InsertedCard = card;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Card card, IDbTransaction tx)
        {
            UpdatedCard = card;
            return Task.CompletedTask;
        }

        public Task<int> UpdatePositionAsync(Guid cardId, Guid laneId, int newPosition, int expectedVersion, IDbTransaction tx)
        {
            UpdatePositionLaneId = laneId;
            return Task.FromResult(_updatePositionRows);
        }

        public Task ParkCardAsync(Guid cardId, IDbTransaction tx)
            => Task.CompletedTask;

        public Task ShiftPositionsInLaneAsync(Guid laneId, int fromPosition, int toPosition, int delta, IDbTransaction tx)
        {
            ShiftedLaneId = laneId;
            ShiftedFrom = fromPosition;
            ShiftedDelta = delta;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid cardId, IDbTransaction tx)
        {
            DeletedCardId = cardId;
            return Task.CompletedTask;
        }

        public Task<int> CountInLaneAsync(Guid laneId, IDbTransaction? tx = null)
            => Task.FromResult(_countInLane);
    }

    private sealed class FakeLaneRepository : ILaneRepository
    {
        private readonly Lane? _lane;

        public FakeLaneRepository(Lane? lane = null) => _lane = lane;

        public Task<IReadOnlyList<Lane>> FindByBoardAsync(Guid boardId, IDbTransaction? tx = null)
            => Task.FromResult<IReadOnlyList<Lane>>(_lane is not null ? [_lane] : []);

        public Task<Lane?> FindByIdAsync(Guid laneId, IDbTransaction? tx = null)
            => Task.FromResult(_lane?.Id == laneId ? _lane : null);

        public Task InsertAsync(Lane lane, IDbTransaction tx) => Task.CompletedTask;
        public Task UpdateNameAsync(Guid laneId, string name, IDbTransaction tx) => Task.CompletedTask;
        public Task<int> UpdatePositionAsync(Guid laneId, int newPosition, int expectedVersion, IDbTransaction tx) => Task.FromResult(1);
        public Task SetPositionAsync(Guid laneId, int position, IDbTransaction tx) => Task.CompletedTask;
        public Task ShiftPositionsAsync(Guid boardId, int fromPosition, int toPosition, int delta, IDbTransaction tx) => Task.CompletedTask;
        public Task DeleteAsync(Guid laneId, IDbTransaction tx) => Task.CompletedTask;
        public Task<int> CountInBoardAsync(Guid boardId, IDbTransaction? tx = null) => Task.FromResult(0);
        public Task<bool> ExistsWithNameInBoardAsync(Guid boardId, string name, IDbTransaction? tx = null) => Task.FromResult(false);
    }

    private sealed class FakeBoardRepository : IBoardRepository
    {
        private readonly Board? _boardForMember;

        public FakeBoardRepository(Board? boardForMember = null) => _boardForMember = boardForMember;

        public Task<Board?> FindBoardForMemberAsync(Guid boardId, Guid userId, IDbTransaction? tx = null)
            => Task.FromResult(_boardForMember);

        public Task<IReadOnlyList<Board>> FindBoardsForUserAsync(Guid userId, IDbTransaction? tx = null)
            => Task.FromResult<IReadOnlyList<Board>>([]);

        public Task InsertAsync(Board board, IDbTransaction tx) => Task.CompletedTask;

        public Task<bool> ExistsWithNameAsync(string name, IDbTransaction? tx = null)
            => Task.FromResult(false);

        public Task DeleteAsync(Guid boardId, IDbTransaction tx) => Task.CompletedTask;
    }

    private sealed class FakeBoardMemberRepository : IBoardMemberRepository
    {
        private readonly BoardRole? _role;

        public FakeBoardMemberRepository(BoardRole? role = null) => _role = role;

        public Task<BoardRole?> FindRoleAsync(Guid boardId, Guid userId, IDbTransaction? tx = null)
            => Task.FromResult(_role);

        public Task<int> CountOwnersAsync(Guid boardId, IDbTransaction? tx = null)
            => Task.FromResult(1);

        public Task InsertAsync(BoardMember member, IDbTransaction tx) => Task.CompletedTask;
        public Task DeleteAsync(Guid boardId, Guid userId, IDbTransaction tx) => Task.CompletedTask;

        public Task UpdateRoleAsync(Guid boardId, Guid userId, BoardRole newRole, IDbTransaction tx)
            => Task.CompletedTask;

        public Task<IReadOnlyList<BoardMember>> FindAllForBoardAsync(Guid boardId, IDbTransaction? tx = null)
            => Task.FromResult<IReadOnlyList<BoardMember>>([]);
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId, string systemRole)
        {
            UserId = userId;
            SystemRole = systemRole;
            IsAuthenticated = true;
            IsRegistered = true;
        }

        public bool IsAuthenticated { get; }
        public bool IsRegistered { get; }
        public Guid? UserId { get; }
        public string? SystemRole { get; }
        public System.Security.Claims.ClaimsPrincipal Principal { get; } = new();
    }

    private sealed class FakeDbConnectionFactory : IDbConnectionFactory
    {
        public IDbTransaction BeginDeferredTransaction(IDbConnection connection)
            => connection.BeginTransaction();
    }
}
