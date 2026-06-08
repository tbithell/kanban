using System.Data;
using FluentAssertions;
using Kanban.Business.Services;
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
using static Kanban.Tests.Unit.Builders.LaneBuilder;

namespace Kanban.Tests.Unit.Business;

public sealed class LaneServiceTests
{
    private readonly Guid _boardId = Guid.NewGuid();
    private readonly Guid _ownerUserId = Guid.NewGuid();

    private LaneService CreateSut(
        ILaneRepository? laneRepo = null,
        IBoardRepository? boardRepo = null,
        IBoardMemberRepository? memberRepo = null,
        FakeCurrentUserService? currentUser = null,
        IAuthorizationService? authService = null)
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var board = ABoard().WithId(_boardId).Build();
        return new LaneService(
            laneRepo ?? new FakeLaneRepository(),
            boardRepo ?? new FakeBoardRepository(boardForMember: board),
            memberRepo ?? new FakeBoardMemberRepository(BoardRole.Owner),
            currentUser ?? new FakeCurrentUserService(_ownerUserId, "standard"),
            authService ?? BuildSucceedingAuthService(),
            connection,
            new FakeDbConnectionFactory(),
            NullLogger<LaneService>.Instance);
    }

    private static IAuthorizationService BuildSucceedingAuthService()
    {
        var mock = new Mock<IAuthorizationService>();
        mock.Setup(s => s.AuthorizeAsync(
                It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()))
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
        return mock.Object;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_UniqueName_AppendsAtPositionN1()
    {
        var laneRepo = new FakeLaneRepository(countInBoard: 2, nameExistsInBoard: false);
        var sut = CreateSut(laneRepo: laneRepo);

        var result = await sut.CreateAsync(_boardId, new CreateLaneRequest("New Lane"));

        result.Name.Should().Be("New Lane");
        result.Position.Should().Be(3);
        laneRepo.InsertedLane.Should().NotBeNull();
        laneRepo.InsertedLane!.Position.Should().Be(3);
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        var laneRepo = new FakeLaneRepository(nameExistsInBoard: true);
        var sut = CreateSut(laneRepo: laneRepo);

        var act = () => sut.CreateAsync(_boardId, new CreateLaneRequest("Existing Lane"));

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "lane.name_conflict");
    }

    [Fact]
    public async Task CreateAsync_NonExistentBoard_ThrowsNotFoundException()
    {
        var boardRepo = new FakeBoardRepository(boardForMember: null);
        var sut = CreateSut(boardRepo: boardRepo);

        var act = () => sut.CreateAsync(Guid.NewGuid(), new CreateLaneRequest("Lane"));

        await act.Should().ThrowAsync<NotFoundException>()
            .Where(e => e.Code == "board.not_found");
    }

    [Fact]
    public async Task CreateAsync_ViewerCaller_ThrowsForbiddenException()
    {
        var sut = CreateSut(authService: BuildFailingAuthService());

        var act = () => sut.CreateAsync(_boardId, new CreateLaneRequest("Lane"));

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ── Rename ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RenameAsync_UniqueName_ReturnsUpdatedLane()
    {
        var laneId = Guid.NewGuid();
        var lane = ALane().WithId(laneId).OnBoard(_boardId).WithName("Old Name").Build();
        var laneRepo = new FakeLaneRepository(lane: lane, nameExistsInBoard: false);
        var sut = CreateSut(laneRepo: laneRepo);

        var result = await sut.RenameAsync(_boardId, laneId, new RenameLaneRequest("New Name"));

        result.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task RenameAsync_DuplicateName_ThrowsConflictException()
    {
        var laneId = Guid.NewGuid();
        var lane = ALane().WithId(laneId).OnBoard(_boardId).Build();
        var laneRepo = new FakeLaneRepository(lane: lane, nameExistsInBoard: true);
        var sut = CreateSut(laneRepo: laneRepo);

        var act = () => sut.RenameAsync(_boardId, laneId, new RenameLaneRequest("Duplicate"));

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "lane.name_conflict");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingLane_DeletesAndShiftsPositions()
    {
        var laneId = Guid.NewGuid();
        var lane = ALane().WithId(laneId).OnBoard(_boardId).AtPosition(2).Build();
        var laneRepo = new FakeLaneRepository(lane: lane, countInBoard: 3);
        var sut = CreateSut(laneRepo: laneRepo);

        await sut.DeleteAsync(_boardId, laneId);

        laneRepo.DeletedLaneId.Should().Be(laneId);
        laneRepo.ShiftedFrom.Should().Be(3);
    }

    [Fact]
    public async Task DeleteAsync_PositionsGaplessAfterDelete()
    {
        var laneId = Guid.NewGuid();
        var lane = ALane().WithId(laneId).OnBoard(_boardId).AtPosition(1).Build();
        var laneRepo = new FakeLaneRepository(lane: lane, countInBoard: 3);
        var sut = CreateSut(laneRepo: laneRepo);

        await sut.DeleteAsync(_boardId, laneId);

        laneRepo.ShiftedDelta.Should().Be(-1);
    }

    // ── Move ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MoveAsync_ValidMove_UpdatesPositionWithVersionCheck()
    {
        var laneId = Guid.NewGuid();
        var lane = ALane().WithId(laneId).OnBoard(_boardId).AtPosition(1).WithVersion(1).Build();
        var laneRepo = new FakeLaneRepository(lane: lane, updatePositionRows: 1);
        var sut = CreateSut(laneRepo: laneRepo);

        await sut.MoveAsync(_boardId, laneId, new MoveLaneRequest(3, 1));

        laneRepo.MovedToPosition.Should().Be(3);
    }

    [Fact]
    public async Task MoveAsync_VersionMismatch_ThrowsConflictException()
    {
        var laneId = Guid.NewGuid();
        var lane = ALane().WithId(laneId).OnBoard(_boardId).WithVersion(5).Build();
        var laneRepo = new FakeLaneRepository(lane: lane, updatePositionRows: 0);
        var sut = CreateSut(laneRepo: laneRepo);

        var act = () => sut.MoveAsync(_boardId, laneId, new MoveLaneRequest(2, 1));

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "lane.version_conflict");
    }

    [Fact]
    public async Task MoveAsync_PositionsGaplessAfterReorder()
    {
        var laneId = Guid.NewGuid();
        var lane = ALane().WithId(laneId).OnBoard(_boardId).AtPosition(3).WithVersion(1).Build();
        var laneRepo = new FakeLaneRepository(lane: lane, countInBoard: 4, updatePositionRows: 1);
        var sut = CreateSut(laneRepo: laneRepo);

        await sut.MoveAsync(_boardId, laneId, new MoveLaneRequest(1, 1));

        laneRepo.ShiftedFrom.Should().BeGreaterThan(0);
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeLaneRepository : ILaneRepository
    {
        private readonly Lane? _lane;
        private readonly bool _nameExistsInBoard;
        private readonly int _countInBoard;
        private readonly int _updatePositionRows;

        public Lane? InsertedLane { get; private set; }
        public Guid? DeletedLaneId { get; private set; }
        public int ShiftedFrom { get; private set; }
        public int ShiftedDelta { get; private set; }
        public int MovedToPosition { get; private set; }

        public FakeLaneRepository(
            Lane? lane = null,
            bool nameExistsInBoard = false,
            int countInBoard = 0,
            int updatePositionRows = 1)
        {
            _lane = lane;
            _nameExistsInBoard = nameExistsInBoard;
            _countInBoard = countInBoard;
            _updatePositionRows = updatePositionRows;
        }

        public Task<IReadOnlyList<Lane>> FindByBoardAsync(Guid boardId, IDbTransaction? tx = null)
            => Task.FromResult<IReadOnlyList<Lane>>(_lane is not null ? [_lane] : []);

        public Task<Lane?> FindByIdAsync(Guid laneId, IDbTransaction? tx = null)
            => Task.FromResult(_lane?.Id == laneId ? _lane : null);

        public Task InsertAsync(Lane lane, IDbTransaction tx)
        {
            InsertedLane = lane;
            return Task.CompletedTask;
        }

        public Task UpdateNameAsync(Guid laneId, string name, IDbTransaction tx)
            => Task.CompletedTask;

        public Task<int> UpdatePositionAsync(Guid laneId, int newPosition, int expectedVersion, IDbTransaction tx)
        {
            MovedToPosition = newPosition;
            return Task.FromResult(_updatePositionRows);
        }

        public Task ShiftPositionsAsync(Guid boardId, int fromPosition, int toPosition, int delta, IDbTransaction tx)
        {
            ShiftedFrom = fromPosition;
            ShiftedDelta = delta;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid laneId, IDbTransaction tx)
        {
            DeletedLaneId = laneId;
            return Task.CompletedTask;
        }

        public Task<int> CountInBoardAsync(Guid boardId, IDbTransaction? tx = null)
            => Task.FromResult(_countInBoard);

        public Task<bool> ExistsWithNameInBoardAsync(Guid boardId, string name, IDbTransaction? tx = null)
            => Task.FromResult(_nameExistsInBoard);
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

    private sealed class FakeCurrentUserService : Kanban.Api.Auth.ICurrentUserService
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
    }

    private sealed class FakeDbConnectionFactory : IDbConnectionFactory
    {
        public IDbTransaction BeginDeferredTransaction(IDbConnection connection)
            => connection.BeginTransaction();
    }
}
