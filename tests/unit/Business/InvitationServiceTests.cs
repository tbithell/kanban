using System.Data;
using FluentAssertions;
using Kanban.Business.Services;
using Kanban.Contracts;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Domain.Events;
using Kanban.Domain.Exceptions;
using Kanban.Tests.Unit.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kanban.Tests.Unit.Business;

public sealed class InvitationServiceTests
{
    private static InvitationService CreateSut(
        FakeInvitationRepository invitationRepo,
        FakeUserRepository userRepo,
        FakeAuthEventRepository authEventRepo)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new InvitationService(
            userRepo, invitationRepo, authEventRepo, connection,
            NullLogger<InvitationService>.Instance);
    }

    [Fact]
    public async Task IssueAsync_NewEmail_CreatesInvitationAndLogsInvitationIssuedEvent()
    {
        var invitationRepo = new FakeInvitationRepository(activeByEmail: null);
        var userRepo = new FakeUserRepository(byEmail: null);
        var authEventRepo = new FakeAuthEventRepository();
        var sut = CreateSut(invitationRepo, userRepo, authEventRepo);

        var (response, isNew) = await sut.IssueAsync(
            "newuser@example.com", Guid.NewGuid(), SystemRole.Admin, "http://localhost:5173");

        isNew.Should().BeTrue();
        response.Token.Should().NotBeNullOrEmpty();
        response.RedemptionLink.Should().Contain("/accept/");
        response.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromSeconds(10));
        invitationRepo.InsertedInvitations.Should().ContainSingle();
        authEventRepo.RecordedEvents.Should().ContainSingle(e =>
            e.EventType == AuthEventType.InvitationIssued);
    }

    [Fact]
    public async Task IssueAsync_ActiveInviteExists_RefreshesTokenAndReturnsWithoutNewRecord()
    {
        var existing = InvitationBuilder.AnInvitation().ForEmail("person@example.com").Build();
        var invitationRepo = new FakeInvitationRepository(activeByEmail: existing);
        var userRepo = new FakeUserRepository(byEmail: null);
        var authEventRepo = new FakeAuthEventRepository();
        var sut = CreateSut(invitationRepo, userRepo, authEventRepo);

        var (response, isNew) = await sut.IssueAsync(
            "person@example.com", Guid.NewGuid(), SystemRole.Admin, "http://localhost:5173");

        isNew.Should().BeFalse();
        response.Token.Should().NotBeNullOrEmpty();
        response.RedemptionLink.Should().Contain("/accept/");
        invitationRepo.InsertedInvitations.Should().BeEmpty("no new record should be created");
        invitationRepo.RefreshedIds.Should().ContainSingle(id => id == existing.Id);
    }

    [Fact]
    public async Task IssueAsync_ExpiredInviteForEmail_CreatesNewInvitation()
    {
        var invitationRepo = new FakeInvitationRepository(activeByEmail: null);
        var userRepo = new FakeUserRepository(byEmail: null);
        var authEventRepo = new FakeAuthEventRepository();
        var sut = CreateSut(invitationRepo, userRepo, authEventRepo);

        var (response, isNew) = await sut.IssueAsync(
            "expired@example.com", Guid.NewGuid(), SystemRole.Admin, "http://localhost:5173");

        isNew.Should().BeTrue();
        invitationRepo.InsertedInvitations.Should().ContainSingle();
        response.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task IssueAsync_EmailAlreadyRegistered_ThrowsConflictException()
    {
        var registeredUser = UserBuilder.AUser().WithEmail("registered@example.com").Build();
        var invitationRepo = new FakeInvitationRepository(activeByEmail: null);
        var userRepo = new FakeUserRepository(byEmail: registeredUser);
        var authEventRepo = new FakeAuthEventRepository();
        var sut = CreateSut(invitationRepo, userRepo, authEventRepo);

        var act = () => sut.IssueAsync(
            "registered@example.com", Guid.NewGuid(), SystemRole.Admin, "http://localhost:5173");

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.Code == "invite.already_registered");
    }

    [Fact]
    public async Task IssueAsync_NonAdminCaller_ThrowsForbiddenException()
    {
        var invitationRepo = new FakeInvitationRepository(activeByEmail: null);
        var userRepo = new FakeUserRepository(byEmail: null);
        var authEventRepo = new FakeAuthEventRepository();
        var sut = CreateSut(invitationRepo, userRepo, authEventRepo);

        var act = () => sut.IssueAsync(
            "anyone@example.com", Guid.NewGuid(), SystemRole.Standard, "http://localhost:5173");

        await act.Should().ThrowAsync<ForbiddenException>()
            .Where(e => e.Code == "invite.forbidden");
    }

    private sealed class FakeInvitationRepository : IInvitationRepository
    {
        private readonly Invitation? _activeByEmail;

        public List<Invitation> InsertedInvitations { get; } = [];
        public List<Guid> RefreshedIds { get; } = [];

        public FakeInvitationRepository(Invitation? activeByEmail)
            => _activeByEmail = activeByEmail;

        public Task<Invitation?> FindByTokenHashAsync(string tokenHash, IDbTransaction? tx = null)
            => Task.FromResult<Invitation?>(null);

        public Task<Invitation?> FindActiveByEmailAsync(string email, IDbTransaction? tx = null)
            => Task.FromResult(_activeByEmail);

        public Task InsertAsync(Invitation invitation, IDbTransaction tx)
        {
            InsertedInvitations.Add(invitation);
            return Task.CompletedTask;
        }

        public Task<bool> TryConsumeAsync(string tokenHash, Guid userId,
            DateTimeOffset consumedAt, IDbTransaction tx) => Task.FromResult(false);

        public Task RefreshTokenAsync(Guid id, string newTokenHash, DateTimeOffset newExpiresAt,
            IDbTransaction tx)
        {
            RefreshedIds.Add(id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User? _byEmail;

        public FakeUserRepository(User? byEmail) => _byEmail = byEmail;

        public Task<User?> FindByGoogleSubAsync(string googleSub, IDbTransaction? tx = null)
            => Task.FromResult<User?>(null);

        public Task<User?> FindByEmailAsync(string email, IDbTransaction? tx = null)
            => Task.FromResult(_byEmail);

        public Task<User?> FindByIdAsync(Guid id, IDbTransaction? tx = null)
            => Task.FromResult<User?>(null);

        public Task InsertAsync(User user, IDbTransaction tx) => Task.CompletedTask;

        public Task LinkGoogleSubAsync(Guid userId, string googleSub, IDbTransaction tx)
            => Task.CompletedTask;

        public Task UpdateLastSignInAsync(Guid userId, DateTimeOffset signedInAt, IDbTransaction tx)
            => Task.CompletedTask;
    }

    private sealed class FakeAuthEventRepository : IAuthEventRepository
    {
        public List<AuthEvent> RecordedEvents { get; } = [];

        public Task RecordAsync(AuthEvent authEvent, IDbTransaction tx)
        {
            RecordedEvents.Add(authEvent);
            return Task.CompletedTask;
        }
    }
}
