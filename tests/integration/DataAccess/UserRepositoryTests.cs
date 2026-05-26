using FluentAssertions;
using Kanban.DataAccess.Repositories;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Tests.Integration.Infrastructure;

namespace Kanban.Tests.Integration.DataAccess;

[Collection(nameof(SqliteCollection))]
public sealed class UserRepositoryTests : IDisposable
{
    private readonly SqliteTestFixture _fixture;
    private readonly System.Data.IDbConnection _connection;
    private readonly UserRepository _sut;

    public UserRepositoryTests(SqliteTestFixture fixture)
    {
        _fixture = fixture;
        _connection = fixture.CreateConnection();
        _sut = new UserRepository(_connection);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task InsertAsync_then_FindByIdAsync_returns_inserted_user()
    {
        var user = BuildUser(googleSub: "sub_insert_by_id");
        using var tx = _connection.BeginTransaction();
        await _sut.InsertAsync(user, tx);
        tx.Commit();

        var found = await _sut.FindByIdAsync(user.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(user.Id);
        found.Email.Should().Be(user.Email);
        found.DisplayName.Should().Be(user.DisplayName);
        found.SystemRole.Should().Be(user.SystemRole);
        found.GoogleSub.Should().Be(user.GoogleSub);
    }

    [Fact]
    public async Task FindByIdAsync_returns_null_when_user_does_not_exist()
    {
        var missingId = Guid.NewGuid();
        var result = await _sut.FindByIdAsync(missingId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task InsertAsync_then_FindByEmailAsync_returns_inserted_user()
    {
        var user = BuildUser(email: $"byemail_{Guid.NewGuid():N}@test.local");
        using var tx = _connection.BeginTransaction();
        await _sut.InsertAsync(user, tx);
        tx.Commit();

        var found = await _sut.FindByEmailAsync(user.Email);

        found.Should().NotBeNull();
        found!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task FindByEmailAsync_returns_null_when_user_does_not_exist()
    {
        var result = await _sut.FindByEmailAsync("nobody@missing.local");
        result.Should().BeNull();
    }

    [Fact]
    public async Task InsertAsync_then_FindByGoogleSubAsync_returns_inserted_user()
    {
        var googleSub = $"sub_{Guid.NewGuid():N}";
        var user = BuildUser(googleSub: googleSub);
        using var tx = _connection.BeginTransaction();
        await _sut.InsertAsync(user, tx);
        tx.Commit();

        var found = await _sut.FindByGoogleSubAsync(googleSub);

        found.Should().NotBeNull();
        found!.GoogleSub.Should().Be(googleSub);
        found.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task FindByGoogleSubAsync_returns_null_when_sub_does_not_exist()
    {
        var result = await _sut.FindByGoogleSubAsync("sub_that_does_not_exist");
        result.Should().BeNull();
    }

    [Fact]
    public async Task LinkGoogleSubAsync_sets_google_sub_on_existing_user()
    {
        var user = BuildUser(googleSub: null);
        using (var tx = _connection.BeginTransaction())
        {
            await _sut.InsertAsync(user, tx);
            tx.Commit();
        }

        var newSub = $"linked_sub_{Guid.NewGuid():N}";
        using (var tx = _connection.BeginTransaction())
        {
            await _sut.LinkGoogleSubAsync(user.Id, newSub, tx);
            tx.Commit();
        }

        var updated = await _sut.FindByIdAsync(user.Id);
        updated!.GoogleSub.Should().Be(newSub);
    }

    [Fact]
    public async Task UpdateLastSignInAsync_sets_last_sign_in_timestamp()
    {
        var user = BuildUser();
        using (var tx = _connection.BeginTransaction())
        {
            await _sut.InsertAsync(user, tx);
            tx.Commit();
        }

        var signInTime = DateTimeOffset.UtcNow.AddSeconds(-1);
        using (var tx = _connection.BeginTransaction())
        {
            await _sut.UpdateLastSignInAsync(user.Id, signInTime, tx);
            tx.Commit();
        }

        var updated = await _sut.FindByIdAsync(user.Id);
        updated!.LastSignInAt.Should().NotBeNull();
        updated.LastSignInAt!.Value.Should().BeCloseTo(signInTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task InsertAsync_preserves_null_google_sub()
    {
        var user = BuildUser(googleSub: null);
        using var tx = _connection.BeginTransaction();
        await _sut.InsertAsync(user, tx);
        tx.Commit();

        var found = await _sut.FindByIdAsync(user.Id);
        found!.GoogleSub.Should().BeNull();
    }

    [Fact]
    public async Task InsertAsync_preserves_registered_at_timestamp()
    {
        var registeredAt = new DateTimeOffset(2025, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var user = BuildUser(registeredAt: registeredAt);
        using var tx = _connection.BeginTransaction();
        await _sut.InsertAsync(user, tx);
        tx.Commit();

        var found = await _sut.FindByIdAsync(user.Id);
        found!.RegisteredAt.Should().Be(registeredAt);
    }

    private static User BuildUser(
        string? email = null,
        string? googleSub = "default-sub",
        SystemRole systemRole = SystemRole.Standard,
        DateTimeOffset? registeredAt = null) =>
        new(
            id: Guid.NewGuid(),
            email: email ?? $"user_{Guid.NewGuid():N}@test.local",
            displayName: "Test User",
            systemRole: systemRole,
            googleSub: googleSub,
            registeredAt: registeredAt ?? DateTimeOffset.UtcNow,
            lastSignInAt: null);
}
