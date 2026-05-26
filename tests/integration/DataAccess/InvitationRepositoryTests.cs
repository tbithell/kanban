using FluentAssertions;
using Kanban.DataAccess.Repositories;
using Kanban.Domain.Entities;
using Kanban.Domain.Enums;
using Kanban.Domain.ValueObjects;
using Kanban.Tests.Integration.Infrastructure;

namespace Kanban.Tests.Integration.DataAccess;

[Collection(nameof(SqliteCollection))]
public sealed class InvitationRepositoryTests : IDisposable
{
    private readonly SqliteTestFixture _fixture;
    private readonly System.Data.IDbConnection _connection;
    private readonly InvitationRepository _sut;
    private readonly UserRepository _users;
    private readonly Guid _issuerId;

    public InvitationRepositoryTests(SqliteTestFixture fixture)
    {
        _fixture = fixture;
        _connection = fixture.CreateConnection();
        _sut = new InvitationRepository(_connection);
        _users = new UserRepository(_connection);
        _issuerId = SeedIssuer();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task InsertAsync_then_FindByTokenHashAsync_returns_inserted_invitation()
    {
        var token = InvitationToken.Generate();
        var invitation = BuildInvitation(token.Hash);
        using var tx = _connection.BeginTransaction();
        await _sut.InsertAsync(invitation, tx);
        tx.Commit();

        var found = await _sut.FindByTokenHashAsync(token.Hash);

        found.Should().NotBeNull();
        found!.Id.Should().Be(invitation.Id);
        found.Email.Should().Be(invitation.Email);
        found.TokenHash.Should().Be(token.Hash);
        found.IssuedByUserId.Should().Be(_issuerId);
    }

    [Fact]
    public async Task FindByTokenHashAsync_returns_null_when_hash_not_found()
    {
        var result = await _sut.FindByTokenHashAsync("0000000000000000000000000000000000000000000000000000000000000000");
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindActiveByEmailAsync_returns_active_invitation()
    {
        var email = $"active_{Guid.NewGuid():N}@test.local";
        var token = InvitationToken.Generate();
        var invitation = BuildInvitation(token.Hash, email: email,
            expiresAt: DateTimeOffset.UtcNow.AddDays(7));
        using var tx = _connection.BeginTransaction();
        await _sut.InsertAsync(invitation, tx);
        tx.Commit();

        var found = await _sut.FindActiveByEmailAsync(email);

        found.Should().NotBeNull();
        found!.Email.Should().Be(email);
    }

    [Fact]
    public async Task FindActiveByEmailAsync_returns_null_when_invitation_is_expired()
    {
        var email = $"expired_{Guid.NewGuid():N}@test.local";
        var token = InvitationToken.Generate();
        var invitation = BuildInvitation(token.Hash, email: email,
            expiresAt: DateTimeOffset.UtcNow.AddDays(-1));
        using var tx = _connection.BeginTransaction();
        await _sut.InsertAsync(invitation, tx);
        tx.Commit();

        var found = await _sut.FindActiveByEmailAsync(email);

        found.Should().BeNull();
    }

    [Fact]
    public async Task FindActiveByEmailAsync_returns_null_when_no_invitation_exists_for_email()
    {
        var result = await _sut.FindActiveByEmailAsync("nobody_active@test.local");
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryConsumeAsync_returns_true_on_first_call()
    {
        var token = InvitationToken.Generate();
        var invitation = BuildInvitation(token.Hash);
        using (var tx = _connection.BeginTransaction())
        {
            await _sut.InsertAsync(invitation, tx);
            tx.Commit();
        }

        var consumerId = Guid.NewGuid();
        bool result;
        using (var tx = _connection.BeginTransaction())
        {
            result = await _sut.TryConsumeAsync(token.Hash, consumerId, DateTimeOffset.UtcNow, tx);
            tx.Commit();
        }

        result.Should().BeTrue();
    }

    [Fact]
    public async Task TryConsumeAsync_returns_false_on_second_call_with_same_token()
    {
        var token = InvitationToken.Generate();
        var invitation = BuildInvitation(token.Hash);
        using (var tx = _connection.BeginTransaction())
        {
            await _sut.InsertAsync(invitation, tx);
            tx.Commit();
        }

        var firstConsumerId = Guid.NewGuid();
        using (var tx = _connection.BeginTransaction())
        {
            await _sut.TryConsumeAsync(token.Hash, firstConsumerId, DateTimeOffset.UtcNow, tx);
            tx.Commit();
        }

        var secondConsumerId = Guid.NewGuid();
        bool secondResult;
        using (var tx = _connection.BeginTransaction())
        {
            secondResult = await _sut.TryConsumeAsync(
                token.Hash, secondConsumerId, DateTimeOffset.UtcNow, tx);
            tx.Commit();
        }

        secondResult.Should().BeFalse();
    }

    [Fact]
    public async Task TryConsumeAsync_returns_false_when_token_is_expired()
    {
        var token = InvitationToken.Generate();
        var invitation = BuildInvitation(token.Hash,
            expiresAt: DateTimeOffset.UtcNow.AddDays(-1));
        using (var tx = _connection.BeginTransaction())
        {
            await _sut.InsertAsync(invitation, tx);
            tx.Commit();
        }

        bool result;
        using (var tx = _connection.BeginTransaction())
        {
            result = await _sut.TryConsumeAsync(
                token.Hash, Guid.NewGuid(), DateTimeOffset.UtcNow, tx);
            tx.Commit();
        }

        result.Should().BeFalse();
    }

    [Fact]
    public async Task InsertAsync_preserves_all_fields()
    {
        var token = InvitationToken.Generate();
        var issuedAt = new DateTimeOffset(2025, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var expiresAt = issuedAt.AddDays(7);
        var invitation = BuildInvitation(token.Hash, issuedAt: issuedAt, expiresAt: expiresAt);
        using var tx = _connection.BeginTransaction();
        await _sut.InsertAsync(invitation, tx);
        tx.Commit();

        var found = await _sut.FindByTokenHashAsync(token.Hash);

        found!.IssuedAt.Should().Be(issuedAt);
        found.ExpiresAt.Should().Be(expiresAt);
        found.ConsumedAt.Should().BeNull();
        found.ConsumedByUserId.Should().BeNull();
    }

    private Guid SeedIssuer()
    {
        var issuer = new User(
            id: Guid.NewGuid(),
            email: $"issuer_{Guid.NewGuid():N}@test.local",
            displayName: "Issuer",
            systemRole: SystemRole.Admin,
            googleSub: null,
            registeredAt: DateTimeOffset.UtcNow,
            lastSignInAt: null);

        using var tx = _connection.BeginTransaction();
        _users.InsertAsync(issuer, tx).GetAwaiter().GetResult();
        tx.Commit();
        return issuer.Id;
    }

    private Invitation BuildInvitation(
        string tokenHash,
        string? email = null,
        DateTimeOffset? issuedAt = null,
        DateTimeOffset? expiresAt = null) =>
        new(
            id: Guid.NewGuid(),
            email: email ?? $"invitee_{Guid.NewGuid():N}@test.local",
            issuedByUserId: _issuerId,
            tokenHash: tokenHash,
            issuedAt: issuedAt ?? DateTimeOffset.UtcNow,
            expiresAt: expiresAt ?? DateTimeOffset.UtcNow.AddDays(7),
            consumedAt: null,
            consumedByUserId: null);
}
