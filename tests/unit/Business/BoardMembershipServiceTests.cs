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
using static Kanban.Tests.Unit.Builders.BoardMemberBuilder;

namespace Kanban.Tests.Unit.Business;

public sealed class BoardMembershipServiceTests
{
    private readonly Guid _ownerUserId = Guid.NewGuid();
    private readonly Guid _boardId = Guid.NewGuid();

    private BoardMembershipService CreateSut(
        IBoardMemberRepository? boardMemberRepo = null,
        IInvitationService? invitationService = null,
        IUserRepository? userRepo = null,
        ICurrentUserService? currentUser = null,
        IAuthorizationService? authService = null)
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new BoardMembershipService(
            boardMemberRepo ?? new FakeBoardMemberRepository(ownerCount: 1, role: BoardRole.Owner),
            invitationService ?? new FakeInvitationService(),
            userRepo ?? new FakeUserRepository(),
            currentUser ?? new FakeCurrentUserService(_ownerUserId, "standard"),
            authService ?? BuildSucceedingAuthService(),
            connection,
            new FakeDbConnectionFactory(),
            NullLogger<BoardMembershipService>.Instance);
    }

    private static IAuthorizationService BuildSucceedingAuthService()
    {
        var mock = new Mock<IAuthorizationService>();
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
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Failed());
        return mock.Object;
    }

    // ── ListMembersAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListMembersAsync_BoardHasMembers_ReturnsMembersOrderedByJoinedAt()
    {
        var now = DateTimeOffset.UtcNow;
        var member1 = ABoardMember().OnBoard(_boardId).ForUser(Guid.NewGuid())
            .JoinedAt(now.AddMinutes(-10)).Build();
        var member2 = ABoardMember().OnBoard(_boardId).ForUser(Guid.NewGuid())
            .JoinedAt(now).Build();
        var boardMemberRepo = new FakeBoardMemberRepository(
            members: [member2, member1],
            ownerCount: 1,
            role: BoardRole.Member);
        var userRepo = new FakeUserRepository(
            users: [
                BuildUser(member1.UserId, "alice@test.local"),
                BuildUser(member2.UserId, "bob@test.local"),
            ]);
        var sut = CreateSut(
            boardMemberRepo: boardMemberRepo,
            userRepo: userRepo,
            currentUser: new FakeCurrentUserService(_ownerUserId, "standard"));

        var result = await sut.ListMembersAsync(_boardId);

        result.Should().HaveCount(2);
        result[0].JoinedAt.Should().Be(member1.JoinedAt);
        result[1].JoinedAt.Should().Be(member2.JoinedAt);
    }

    // ── InviteAsync ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task InviteAsync_OwnerInvitesNewEmail_CallsIssueAsync()
    {
        var fakeInvitation = new FakeInvitationService();
        var sut = CreateSut(invitationService: fakeInvitation);
        var request = new InviteBoardMemberRequest("new@test.local", BoardRoleDto.Member);

        await sut.InviteAsync(_boardId, request, "http://localhost:5173");

        fakeInvitation.IssueCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InviteAsync_EmailAlreadyMember_ThrowsConflictException()
    {
        var existingUserId = Guid.NewGuid();
        var userRepo = new FakeUserRepository(users: [BuildUser(existingUserId, "existing@test.local")]);
        var boardMemberRepo = new FakeBoardMemberRepository(
            ownerCount: 1,
            role: BoardRole.Owner,
            existingMemberUserId: existingUserId);
        var sut = CreateSut(boardMemberRepo: boardMemberRepo, userRepo: userRepo);
        var request = new InviteBoardMemberRequest("existing@test.local", BoardRoleDto.Member);

        var act = () => sut.InviteAsync(_boardId, request, "http://localhost:5173");

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "member.already_member");
    }

    [Fact]
    public async Task InviteAsync_CallerLacksManageMembersPermission_ThrowsForbiddenException()
    {
        var sut = CreateSut(authService: BuildFailingAuthService());
        var request = new InviteBoardMemberRequest("newperson@test.local", BoardRoleDto.Member);

        var act = () => sut.InviteAsync(_boardId, request, "http://localhost:5173");

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task InviteAsync_MemberCallerRole_ThrowsForbiddenException()
    {
        var sut = CreateSut(authService: BuildFailingAuthService());
        var request = new InviteBoardMemberRequest("someone@test.local", BoardRoleDto.Viewer);

        var act = () => sut.InviteAsync(_boardId, request, "http://localhost:5173");

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // ── ChangeRoleAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ChangeRoleAsync_OwnerChangesOtherMemberRole_UpdatesRole()
    {
        var targetUserId = Guid.NewGuid();
        var boardMemberRepo = new FakeBoardMemberRepository(
            ownerCount: 2,
            role: BoardRole.Owner,
            existingMemberUserId: targetUserId);
        var sut = CreateSut(boardMemberRepo: boardMemberRepo);
        var request = new ChangeMemberRoleRequest(BoardRoleDto.Viewer);

        await sut.ChangeRoleAsync(_boardId, targetUserId, request);

        boardMemberRepo.UpdatedRole.Should().Be(BoardRole.Viewer);
    }

    [Fact]
    public async Task ChangeRoleAsync_LastOwnerDowngrade_ThrowsBusinessRuleException()
    {
        var boardMemberRepo = new FakeBoardMemberRepository(ownerCount: 1, role: BoardRole.Owner);
        var sut = CreateSut(boardMemberRepo: boardMemberRepo);
        var request = new ChangeMemberRoleRequest(BoardRoleDto.Member);

        var act = () => sut.ChangeRoleAsync(_boardId, _ownerUserId, request);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "member.last_owner");
    }

    [Fact]
    public async Task ChangeRoleAsync_ChangeRoleToOwner_UpdatesRole()
    {
        var targetUserId = Guid.NewGuid();
        var boardMemberRepo = new FakeBoardMemberRepository(
            ownerCount: 1,
            role: BoardRole.Owner,
            existingMemberUserId: targetUserId);
        var sut = CreateSut(boardMemberRepo: boardMemberRepo);
        var request = new ChangeMemberRoleRequest(BoardRoleDto.Owner);

        await sut.ChangeRoleAsync(_boardId, targetUserId, request);

        boardMemberRepo.UpdatedRole.Should().Be(BoardRole.Owner);
    }

    [Fact]
    public async Task ChangeRoleAsync_TargetNotMember_ThrowsNotFoundException()
    {
        var targetUserId = Guid.NewGuid();
        var boardMemberRepo = new FakeBoardMemberRepository(
            ownerCount: 1,
            role: BoardRole.Owner,
            nonMemberUserId: targetUserId);
        var sut = CreateSut(boardMemberRepo: boardMemberRepo);
        var request = new ChangeMemberRoleRequest(BoardRoleDto.Member);

        var act = () => sut.ChangeRoleAsync(_boardId, targetUserId, request);

        await act.Should().ThrowAsync<NotFoundException>()
            .Where(e => e.Code == "member.not_found");
    }

    // ── RemoveMemberAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveMemberAsync_OwnerRemovesAnotherMember_DeletesMember()
    {
        var targetUserId = Guid.NewGuid();
        var boardMemberRepo = new FakeBoardMemberRepository(
            ownerCount: 1,
            role: BoardRole.Owner,
            existingMemberUserId: targetUserId);
        var sut = CreateSut(boardMemberRepo: boardMemberRepo);

        await sut.RemoveMemberAsync(_boardId, targetUserId);

        boardMemberRepo.DeleteCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveMemberAsync_LastOwner_ThrowsBusinessRuleException()
    {
        var boardMemberRepo = new FakeBoardMemberRepository(ownerCount: 1, role: BoardRole.Owner);
        var sut = CreateSut(boardMemberRepo: boardMemberRepo);

        var act = () => sut.RemoveMemberAsync(_boardId, _ownerUserId);

        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "member.last_owner");
    }

    [Fact]
    public async Task RemoveMemberAsync_TargetNotMember_ThrowsNotFoundException()
    {
        var targetUserId = Guid.NewGuid();
        var boardMemberRepo = new FakeBoardMemberRepository(
            ownerCount: 1,
            role: BoardRole.Owner,
            nonMemberUserId: targetUserId);
        var sut = CreateSut(boardMemberRepo: boardMemberRepo);

        var act = () => sut.RemoveMemberAsync(_boardId, targetUserId);

        await act.Should().ThrowAsync<NotFoundException>()
            .Where(e => e.Code == "member.not_found");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────

    private static User BuildUser(Guid id, string email) =>
        new(id: id,
            email: email,
            displayName: email,
            systemRole: SystemRole.Standard,
            googleSub: $"sub-{id:N}",
            registeredAt: DateTimeOffset.UtcNow,
            lastSignInAt: null);

    // ── Fakes ─────────────────────────────────────────────────────────────────────

    private sealed class FakeBoardMemberRepository : IBoardMemberRepository
    {
        private readonly int _ownerCount;
        private readonly BoardRole? _role;
        private readonly Guid? _existingMemberUserId;
        private readonly Guid? _nonMemberUserId;
        private readonly IReadOnlyList<BoardMember> _members;

        public bool DeleteCalled { get; private set; }
        public BoardRole? UpdatedRole { get; private set; }

        public FakeBoardMemberRepository(
            int ownerCount = 1,
            BoardRole? role = null,
            Guid? existingMemberUserId = null,
            IReadOnlyList<BoardMember>? members = null,
            Guid? nonMemberUserId = null)
        {
            _ownerCount = ownerCount;
            _role = role;
            _existingMemberUserId = existingMemberUserId;
            _nonMemberUserId = nonMemberUserId;
            _members = members ?? [];
        }

        public Task<BoardRole?> FindRoleAsync(Guid boardId, Guid userId, IDbTransaction? tx = null)
        {
            if (_nonMemberUserId.HasValue && userId == _nonMemberUserId.Value)
                return Task.FromResult<BoardRole?>(null);
            return Task.FromResult(_existingMemberUserId == userId ? (BoardRole?)BoardRole.Member : _role);
        }

        public Task<int> CountOwnersAsync(Guid boardId, IDbTransaction? tx = null)
            => Task.FromResult(_ownerCount);

        public Task InsertAsync(BoardMember member, IDbTransaction tx)
            => Task.CompletedTask;

        public Task DeleteAsync(Guid boardId, Guid userId, IDbTransaction tx)
        {
            DeleteCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateRoleAsync(Guid boardId, Guid userId, BoardRole newRole, IDbTransaction tx)
        {
            UpdatedRole = newRole;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<BoardMember>> FindAllForBoardAsync(Guid boardId, IDbTransaction? tx = null)
        {
            if (_members.Count > 0)
                return Task.FromResult(_members);

            // Return a minimal member so ChangeRoleAsync can build a DTO after the update.
            var targetId = _existingMemberUserId ?? Guid.NewGuid();
            IReadOnlyList<BoardMember> fallback = [
                new BoardMember(Guid.NewGuid(), boardId, targetId, _role ?? BoardRole.Member, null, DateTimeOffset.UtcNow),
            ];
            return Task.FromResult(fallback);
        }
    }

    private sealed class FakeInvitationService : IInvitationService
    {
        public bool IssueCalled { get; private set; }

        public Task<(IssueInviteResponse Response, bool IsNew)> IssueAsync(
            string email,
            Guid issuedByUserId,
            SystemRole callerRole,
            string frontendBaseUrl,
            Guid? boardId = null,
            BoardRole? boardRole = null,
            CancellationToken cancellationToken = default)
        {
            IssueCalled = true;
            var response = new IssueInviteResponse
            {
                Token = "test-token",
                RedemptionLink = $"{frontendBaseUrl}/accept/test-token",
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
                InvitationId = Guid.NewGuid(),
            };
            return Task.FromResult<(IssueInviteResponse, bool)>((response, true));
        }

        public Task<(User User, Guid? BoardId)> AcceptAsync(
            string rawToken,
            string googleEmail,
            string googleSub,
            string displayName,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly IReadOnlyList<User> _users;

        public FakeUserRepository(IReadOnlyList<User>? users = null) => _users = users ?? [];

        public Task<User?> FindByGoogleSubAsync(string googleSub, IDbTransaction? tx = null)
            => Task.FromResult<User?>(null);

        public Task<User?> FindByEmailAsync(string email, IDbTransaction? tx = null)
            => Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

        public Task<User?> FindByIdAsync(Guid id, IDbTransaction? tx = null)
            => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

        public Task<IReadOnlyList<User>> FindByIdsAsync(IEnumerable<Guid> ids, IDbTransaction? tx = null)
        {
            var idSet = ids.ToHashSet();
            IReadOnlyList<User> result = _users.Where(u => idSet.Contains(u.Id)).ToList();
            return Task.FromResult(result);
        }

        public Task InsertAsync(User user, IDbTransaction tx) => Task.CompletedTask;

        public Task LinkGoogleSubAsync(Guid userId, string googleSub, IDbTransaction tx)
            => Task.CompletedTask;

        public Task UpdateLastSignInAsync(Guid userId, DateTimeOffset signedInAt, IDbTransaction tx)
            => Task.CompletedTask;
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
