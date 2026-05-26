using System.Data;
using System.Security.Claims;
using FluentAssertions;
using Kanban.Business.Services;
using Kanban.DataAccess.Interfaces;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Domain.Events;
using Kanban.Tests.Unit.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kanban.Tests.Unit.Business;

public sealed class AuthServiceTests
{
    private static AuthService CreateSut(IUserRepository userRepo, IAuthEventRepository authEventRepo)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return new AuthService(userRepo, authEventRepo, connection, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task HandleSignInAsync_AdminFirstSignIn_LinksGoogleSubAndAddsClaims()
    {
        var admin = UserBuilder.AUser().AsAdmin().WithoutGoogleSub().WithEmail("admin@example.com").Build();
        var userRepo = new FakeUserRepository(byGoogleSub: null, byEmail: admin);
        var authEventRepo = new FakeAuthEventRepository();
        var sut = CreateSut(userRepo, authEventRepo);
        var identity = new ClaimsIdentity();

        await sut.HandleSignInAsync("google-sub-123", "admin@example.com", identity);

        userRepo.LinkedGoogleSub.Should().Be("google-sub-123");
        identity.FindFirst("user_id")?.Value.Should().Be(admin.Id.ToString());
        identity.FindFirst("system_role")?.Value.Should().Be("admin");
    }

    [Fact]
    public async Task HandleSignInAsync_ReturningUser_AddsClaims()
    {
        var user = UserBuilder.AUser().WithGoogleSub("existing-sub").Build();
        var userRepo = new FakeUserRepository(byGoogleSub: user, byEmail: null);
        var authEventRepo = new FakeAuthEventRepository();
        var sut = CreateSut(userRepo, authEventRepo);
        var identity = new ClaimsIdentity();

        await sut.HandleSignInAsync("existing-sub", user.Email, identity);

        userRepo.LinkedGoogleSub.Should().BeNull("returning user already has a Google sub linked");
        identity.FindFirst("user_id")?.Value.Should().Be(user.Id.ToString());
        identity.FindFirst("system_role")?.Value.Should().Be("standard");
    }

    [Fact]
    public async Task HandleSignInAsync_UnregisteredUser_NoClaimsAdded()
    {
        var userRepo = new FakeUserRepository(byGoogleSub: null, byEmail: null);
        var authEventRepo = new FakeAuthEventRepository();
        var sut = CreateSut(userRepo, authEventRepo);
        var identity = new ClaimsIdentity();

        await sut.HandleSignInAsync("unknown-sub", "stranger@example.com", identity);

        identity.FindFirst("user_id").Should().BeNull();
        identity.FindFirst("system_role").Should().BeNull();
    }

    [Fact]
    public async Task HandleSignInAsync_RegisteredUser_RecordsSignInEvent()
    {
        var user = UserBuilder.AUser().WithGoogleSub("some-sub").Build();
        var userRepo = new FakeUserRepository(byGoogleSub: user, byEmail: null);
        var authEventRepo = new FakeAuthEventRepository();
        var sut = CreateSut(userRepo, authEventRepo);

        await sut.HandleSignInAsync("some-sub", user.Email, new ClaimsIdentity());

        userRepo.UpdatedLastSignInUserId.Should().Be(user.Id);
        authEventRepo.RecordedEvents.Should().ContainSingle(e =>
            e.EventType == AuthEventType.SignIn && e.UserId == user.Id);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User? _byGoogleSub;
        private readonly User? _byEmail;

        public string? LinkedGoogleSub { get; private set; }
        public Guid? UpdatedLastSignInUserId { get; private set; }

        public FakeUserRepository(User? byGoogleSub, User? byEmail)
        {
            _byGoogleSub = byGoogleSub;
            _byEmail = byEmail;
        }

        public Task<User?> FindByGoogleSubAsync(string googleSub, IDbTransaction? tx = null)
            => Task.FromResult(_byGoogleSub);

        public Task<User?> FindByEmailAsync(string email, IDbTransaction? tx = null)
            => Task.FromResult(_byEmail);

        public Task<User?> FindByIdAsync(Guid id, IDbTransaction? tx = null)
            => Task.FromResult<User?>(null);

        public Task InsertAsync(User user, IDbTransaction tx)
            => Task.CompletedTask;

        public Task LinkGoogleSubAsync(Guid userId, string googleSub, IDbTransaction tx)
        {
            LinkedGoogleSub = googleSub;
            return Task.CompletedTask;
        }

        public Task UpdateLastSignInAsync(Guid userId, DateTimeOffset signedInAt, IDbTransaction tx)
        {
            UpdatedLastSignInUserId = userId;
            return Task.CompletedTask;
        }
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
