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
            userRepo, invitationRepo, authEventRepo, connection, new FakeDbConnectionFactory(),
            NullLogger<InvitationService>.Instance);
    }

    private sealed class FakeDbConnectionFactory : IDbConnectionFactory
    {
        public IDbTransaction BeginDeferredTransaction(IDbConnection connection)
            => connection.BeginTransaction();
    }

    // ── IssueAsync tests ──────────────────────────────────────────────────────

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
        var issuedEvt = authEventRepo.RecordedEvents.Should().ContainSingle(e =>
            e.EventType == AuthEventType.InvitationIssued).Subject;
        issuedEvt.Outcome.Should().NotContain("@", "audit events must not contain email addresses");
        issuedEvt.Outcome.Should().NotContain("newuser", "audit events must not contain identifiable strings");
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

    // ── AcceptAsync tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task AcceptAsync_ValidTokenMatchingEmail_CreatesUserAndRecordsAcceptedEvent()
    {
        var invitation = InvitationBuilder.AnInvitation().ForEmail("invitee@example.com").Build();
        var invitationRepo = new FakeInvitationRepository(byTokenHash: invitation, tryConsumeResult: true);
        var userRepo = new FakeUserRepository();
        var authEventRepo = new FakeAuthEventRepository();
        var sut = CreateSut(invitationRepo, userRepo, authEventRepo);

        var user = await sut.AcceptAsync(
            rawToken: "validrawtoken",
            googleEmail: "invitee@example.com",
            googleSub: "google-sub-123",
            displayName: "Invitee User");

        user.Email.Should().Be("invitee@example.com");
        user.SystemRole.Should().Be(SystemRole.Standard);
        user.GoogleSub.Should().Be("google-sub-123");
        userRepo.InsertedUsers.Should().ContainSingle();
        var acceptedEvt = authEventRepo.RecordedEvents.Should().ContainSingle(e =>
            e.EventType == AuthEventType.InvitationAccepted).Subject;
        acceptedEvt.Outcome.Should().NotContain("@", "audit events must not contain email addresses");
        acceptedEvt.Outcome.Should().NotContain("validrawtoken", "audit events must not contain token values");
        acceptedEvt.Outcome.Should().NotContain("Invitee User", "audit events must not contain display names");
    }

    [Fact]
    public async Task AcceptAsync_ExpiredToken_ThrowsNotFoundException()
    {
        // TryConsumeAsync SQL: expires_at > @now → returns false for expired tokens
        var invitationRepo = new FakeInvitationRepository(tryConsumeResult: false);
        var sut = CreateSut(invitationRepo, new FakeUserRepository(), new FakeAuthEventRepository());

        var act = () => sut.AcceptAsync("expiredrawtoken", "user@example.com", "sub", "User");

        await act.Should().ThrowAsync<NotFoundException>()
            .Where(e => e.Code == "invite.invalid");
    }

    [Fact]
    public async Task AcceptAsync_ConsumedToken_ThrowsNotFoundException()
    {
        // TryConsumeAsync SQL: consumed_at IS NULL → returns false for already-consumed tokens
        var invitationRepo = new FakeInvitationRepository(tryConsumeResult: false);
        var sut = CreateSut(invitationRepo, new FakeUserRepository(), new FakeAuthEventRepository());

        var act = () => sut.AcceptAsync("consumedrawtoken", "user@example.com", "sub", "User");

        await act.Should().ThrowAsync<NotFoundException>()
            .Where(e => e.Code == "invite.invalid");
    }

    [Fact]
    public async Task AcceptAsync_NonExistentToken_ThrowsNotFoundException()
    {
        // TryConsumeAsync SQL: no matching row → returns false for fabricated tokens
        var invitationRepo = new FakeInvitationRepository(tryConsumeResult: false);
        var sut = CreateSut(invitationRepo, new FakeUserRepository(), new FakeAuthEventRepository());

        var act = () => sut.AcceptAsync("fabricatedtoken", "user@example.com", "sub", "User");

        await act.Should().ThrowAsync<NotFoundException>()
            .Where(e => e.Code == "invite.invalid");
    }

    [Fact]
    public async Task AcceptAsync_EmailMismatch_ThrowsBusinessRuleException()
    {
        var invitation = InvitationBuilder.AnInvitation().ForEmail("person@example.com").Build();
        var invitationRepo = new FakeInvitationRepository(byTokenHash: invitation, tryConsumeResult: true);
        var sut = CreateSut(invitationRepo, new FakeUserRepository(), new FakeAuthEventRepository());

        var act = () => sut.AcceptAsync("validrawtoken", "different@example.com", "sub", "User");

        await act.Should().ThrowAsync<BusinessRuleException>()
            .Where(e => e.Code == "invite.email_mismatch");
    }

    [Fact]
    public async Task AcceptAsync_ConcurrentSecondCall_ThrowsNotFoundException()
    {
        // Invitation was valid on read, but consumed by another concurrent request before TryConsumeAsync
        var invitation = InvitationBuilder.AnInvitation().ForEmail("user@example.com").Build();
        var invitationRepo = new FakeInvitationRepository(byTokenHash: invitation, tryConsumeResult: false);
        var sut = CreateSut(invitationRepo, new FakeUserRepository(), new FakeAuthEventRepository());

        var act = () => sut.AcceptAsync("alreadyconsumedtoken", "user@example.com", "sub", "User");

        await act.Should().ThrowAsync<NotFoundException>()
            .Where(e => e.Code == "invite.invalid");
    }

    // ── Fakes ─────────────────────────────────────────────────────────────────

    private sealed class FakeInvitationRepository : IInvitationRepository
    {
        private readonly Invitation? _activeByEmail;
        private readonly Invitation? _byTokenHash;
        private readonly bool _tryConsumeResult;

        public List<Invitation> InsertedInvitations { get; } = [];
        public List<Guid> RefreshedIds { get; } = [];

        public FakeInvitationRepository(
            Invitation? activeByEmail = null,
            Invitation? byTokenHash = null,
            bool tryConsumeResult = true)
        {
            _activeByEmail = activeByEmail;
            _byTokenHash = byTokenHash;
            _tryConsumeResult = tryConsumeResult;
        }

        public Task<Invitation?> FindByTokenHashAsync(string tokenHash, IDbTransaction? tx = null)
            => Task.FromResult(_byTokenHash);

        public Task<Invitation?> FindActiveByEmailAsync(string email, IDbTransaction? tx = null)
            => Task.FromResult(_activeByEmail);

        public Task InsertAsync(Invitation invitation, IDbTransaction tx)
        {
            InsertedInvitations.Add(invitation);
            return Task.CompletedTask;
        }

        public Task<bool> TryConsumeAsync(string tokenHash, DateTimeOffset consumedAt,
            IDbTransaction tx)
            => Task.FromResult(_tryConsumeResult);

        public Task RecordConsumerAsync(string tokenHash, Guid consumedByUserId, IDbTransaction tx)
            => Task.CompletedTask;

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

        public List<User> InsertedUsers { get; } = [];

        public FakeUserRepository(User? byEmail = null) => _byEmail = byEmail;

        public Task<User?> FindByGoogleSubAsync(string googleSub, IDbTransaction? tx = null)
            => Task.FromResult<User?>(null);

        public Task<User?> FindByEmailAsync(string email, IDbTransaction? tx = null)
            => Task.FromResult(_byEmail);

        public Task<User?> FindByIdAsync(Guid id, IDbTransaction? tx = null)
            => Task.FromResult<User?>(null);

        public Task InsertAsync(User user, IDbTransaction tx)
        {
            InsertedUsers.Add(user);
            return Task.CompletedTask;
        }

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
