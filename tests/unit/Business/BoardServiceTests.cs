using System.Data;
using FluentAssertions;
using FluentValidation;
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
using static Kanban.Tests.Unit.Builders.BoardMemberBuilder;

namespace Kanban.Tests.Unit.Business;

public sealed class BoardServiceTests
{
    private readonly Guid _adminUserId = Guid.NewGuid();

    private BoardService CreateSut(
        IBoardRepository? boardRepo = null,
        IBoardMemberRepository? boardMemberRepo = null,
        ILaneRepository? laneRepo = null,
        ICardRepository? cardRepo = null,
        ICurrentUserServiceFake? currentUser = null,
        IAuthorizationService? authService = null)
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new BoardService(
            boardRepo ?? new FakeBoardRepository(),
            boardMemberRepo ?? new FakeBoardMemberRepository(),
            laneRepo ?? new FakeLaneRepository(),
            cardRepo ?? new FakeCardRepository(),
            currentUser ?? new FakeCurrentUserService(_adminUserId, "admin"),
            authService ?? BuildSucceedingAuthService(),
            connection,
            new FakeDbConnectionFactory(),
            NullLogger<BoardService>.Instance);
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
    public async Task CreateAsync_AdminWithUniqueName_ReturnsBoardDetailDtoWithOwnerRole()
    {
        var repo = new FakeBoardRepository(nameExists: false);
        var sut = CreateSut(boardRepo: repo);

        var result = await sut.CreateAsync(new CreateBoardRequest("My Board"));

        result.Should().NotBeNull();
        result.Name.Should().Be("My Board");
        result.CallerRole.Should().Be(BoardRoleDto.Owner);
        repo.InsertedBoard.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        var repo = new FakeBoardRepository(nameExists: true);
        var sut = CreateSut(boardRepo: repo);

        var act = () => sut.CreateAsync(new CreateBoardRequest("Duplicate Board"));

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "board.name_conflict");
    }

    [Fact]
    public async Task CreateAsync_NonAdminCaller_ThrowsForbiddenException()
    {
        var sut = CreateSut(
            currentUser: new FakeCurrentUserService(Guid.NewGuid(), "standard"),
            authService: BuildFailingAuthService());

        var act = () => sut.CreateAsync(new CreateBoardRequest("Any Board"));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ── List ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListForCallerAsync_ReturnsBoardsForCurrentUser()
    {
        var boardId = Guid.NewGuid();
        var board = ABoard().WithId(boardId).WithName("Board A").Build();
        var repo = new FakeBoardRepository(boards: [board]);
        var sut = CreateSut(boardRepo: repo);

        var result = await sut.ListForCallerAsync();

        result.Should().ContainSingle(b => b.Id == boardId && b.Name == "Board A");
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_MemberCallerWithExistingBoard_ReturnsBoardDetail()
    {
        var boardId = Guid.NewGuid();
        var board = ABoard().WithId(boardId).WithName("Board X").Build();
        var memberRepo = new FakeBoardMemberRepository(role: BoardRole.Owner);
        var repo = new FakeBoardRepository(boardForMember: board);
        var sut = CreateSut(boardRepo: repo, boardMemberRepo: memberRepo);

        var result = await sut.GetAsync(boardId);

        result.Id.Should().Be(boardId);
        result.Name.Should().Be("Board X");
    }

    [Fact]
    public async Task GetAsync_NonMemberCaller_ThrowsNotFoundException()
    {
        var boardId = Guid.NewGuid();
        var repo = new FakeBoardRepository(boardForMember: null);
        var sut = CreateSut(boardRepo: repo);

        var act = () => sut.GetAsync(boardId);

        await act.Should().ThrowAsync<NotFoundException>()
            .Where(e => e.Code == "board.not_found");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_CallerIsOwner_DeletesBoard()
    {
        var boardId = Guid.NewGuid();
        var board = ABoard().WithId(boardId).Build();
        var memberRepo = new FakeBoardMemberRepository(role: BoardRole.Owner);
        var repo = new FakeBoardRepository(boardForMember: board);
        var sut = CreateSut(boardRepo: repo, boardMemberRepo: memberRepo);

        await sut.DeleteAsync(boardId);

        repo.DeletedBoardId.Should().Be(boardId);
    }

    [Fact]
    public async Task DeleteAsync_CallerIsNotOwner_ThrowsForbiddenException()
    {
        var boardId = Guid.NewGuid();
        var board = ABoard().WithId(boardId).Build();
        var memberRepo = new FakeBoardMemberRepository(role: BoardRole.Member);
        var repo = new FakeBoardRepository(boardForMember: board);
        var sut = CreateSut(
            boardRepo: repo,
            boardMemberRepo: memberRepo,
            authService: BuildFailingAuthService());

        var act = () => sut.DeleteAsync(boardId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeBoardRepository : IBoardRepository
    {
        private readonly bool _nameExists;
        private readonly Board? _boardForMember;
        private readonly IReadOnlyList<Board> _boards;

        public Board? InsertedBoard { get; private set; }
        public Guid? DeletedBoardId { get; private set; }

        public FakeBoardRepository(
            bool nameExists = false,
            Board? boardForMember = null,
            IReadOnlyList<Board>? boards = null)
        {
            _nameExists = nameExists;
            _boardForMember = boardForMember;
            _boards = boards ?? [];
        }

        public Task<Board?> FindBoardForMemberAsync(Guid boardId, Guid userId, IDbTransaction? tx = null)
            => Task.FromResult(_boardForMember);

        public Task<IReadOnlyList<Board>> FindBoardsForUserAsync(Guid userId, IDbTransaction? tx = null)
            => Task.FromResult(_boards);

        public Task InsertAsync(Board board, IDbTransaction tx)
        {
            InsertedBoard = board;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsWithNameAsync(string name, IDbTransaction? tx = null)
            => Task.FromResult(_nameExists);

        public Task DeleteAsync(Guid boardId, IDbTransaction tx)
        {
            DeletedBoardId = boardId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBoardMemberRepository : IBoardMemberRepository
    {
        private readonly BoardRole? _role;

        public FakeBoardMemberRepository(BoardRole? role = null) => _role = role;

        public Task<BoardRole?> FindRoleAsync(Guid boardId, Guid userId, IDbTransaction? tx = null)
            => Task.FromResult(_role);

        public Task<int> CountOwnersAsync(Guid boardId, IDbTransaction? tx = null)
            => Task.FromResult(_role == BoardRole.Owner ? 1 : 0);

        public Task InsertAsync(BoardMember member, IDbTransaction tx)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid boardId, Guid userId, IDbTransaction tx)
            => Task.CompletedTask;

        public Task UpdateRoleAsync(Guid boardId, Guid userId, BoardRole newRole, IDbTransaction tx)
            => Task.CompletedTask;

        public Task<IReadOnlyList<BoardMember>> FindAllForBoardAsync(Guid boardId, IDbTransaction? tx = null)
            => Task.FromResult<IReadOnlyList<BoardMember>>([]);
    }

    private sealed class FakeCurrentUserService : ICurrentUserServiceFake
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

    private sealed class FakeLaneRepository : ILaneRepository
    {
        public Task<IReadOnlyList<Lane>> FindByBoardAsync(Guid boardId, IDbTransaction? tx = null)
            => Task.FromResult<IReadOnlyList<Lane>>([]);

        public Task<Lane?> FindByIdAsync(Guid laneId, IDbTransaction? tx = null)
            => Task.FromResult<Lane?>(null);

        public Task InsertAsync(Lane lane, IDbTransaction tx) => Task.CompletedTask;

        public Task UpdateNameAsync(Guid laneId, string name, IDbTransaction tx) => Task.CompletedTask;

        public Task<int> UpdatePositionAsync(Guid laneId, int newPosition, int expectedVersion, IDbTransaction tx)
            => Task.FromResult(1);

        public Task SetPositionAsync(Guid laneId, int position, IDbTransaction tx) => Task.CompletedTask;

        public Task ShiftPositionsAsync(Guid boardId, int fromPosition, int toPosition, int delta, IDbTransaction tx)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid laneId, IDbTransaction tx) => Task.CompletedTask;

        public Task<int> CountInBoardAsync(Guid boardId, IDbTransaction? tx = null)
            => Task.FromResult(0);

        public Task<bool> ExistsWithNameInBoardAsync(Guid boardId, string name, IDbTransaction? tx = null)
            => Task.FromResult(false);
    }

    private sealed class FakeCardRepository : ICardRepository
    {
        public Task<IReadOnlyList<Card>> FindByLaneAsync(Guid laneId, IDbTransaction? tx = null)
            => Task.FromResult<IReadOnlyList<Card>>([]);

        public Task<Card?> FindByIdAsync(Guid cardId, IDbTransaction? tx = null)
            => Task.FromResult<Card?>(null);

        public Task InsertAsync(Card card, IDbTransaction tx) => Task.CompletedTask;

        public Task UpdateAsync(Card card, IDbTransaction tx) => Task.CompletedTask;

        public Task<int> UpdatePositionAsync(Guid cardId, Guid laneId, int newPosition, int expectedVersion, IDbTransaction tx)
            => Task.FromResult(1);

        public Task ParkCardAsync(Guid cardId, IDbTransaction tx) => Task.CompletedTask;

        public Task ShiftPositionsInLaneAsync(Guid laneId, int fromPosition, int toPosition, int delta, IDbTransaction tx)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid cardId, IDbTransaction tx) => Task.CompletedTask;

        public Task<int> CountInLaneAsync(Guid laneId, IDbTransaction? tx = null)
            => Task.FromResult(0);
    }

    private sealed class FakeDbConnectionFactory : IDbConnectionFactory
    {
        public IDbTransaction BeginDeferredTransaction(IDbConnection connection)
            => connection.BeginTransaction();
    }

    public interface ICurrentUserServiceFake : Kanban.Business.Interfaces.ICurrentUserService { }
}
