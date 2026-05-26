using Kanban.Domain.Entities;
using Kanban.Domain.ValueObjects;

namespace Kanban.Tests.Unit.Builders;

public sealed class InvitationBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _email = $"invitee_{Guid.NewGuid():N}@test.local";
    private Guid _issuedByUserId = Guid.NewGuid();
    private string _tokenHash = InvitationToken.Generate().Hash;
    private DateTimeOffset _issuedAt = DateTimeOffset.UtcNow;
    private DateTimeOffset _expiresAt = DateTimeOffset.UtcNow.AddDays(7);
    private DateTimeOffset? _consumedAt;
    private Guid? _consumedByUserId;

    public static InvitationBuilder AnInvitation() => new();

    public InvitationBuilder ForEmail(string email) { _email = email; return this; }
    public InvitationBuilder IssuedBy(Guid userId) { _issuedByUserId = userId; return this; }
    public InvitationBuilder WithTokenHash(string tokenHash) { _tokenHash = tokenHash; return this; }
    public InvitationBuilder IssuedAt(DateTimeOffset issuedAt) { _issuedAt = issuedAt; return this; }
    public InvitationBuilder WithExpiry(DateTimeOffset expiresAt) { _expiresAt = expiresAt; return this; }

    public InvitationBuilder AsExpired()
    {
        _expiresAt = DateTimeOffset.UtcNow.AddDays(-1);
        return this;
    }

    public InvitationBuilder AsConsumed(Guid? consumedByUserId = null)
    {
        _consumedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        _consumedByUserId = consumedByUserId ?? Guid.NewGuid();
        return this;
    }

    public Invitation Build() =>
        new(
            id: _id,
            email: _email,
            issuedByUserId: _issuedByUserId,
            tokenHash: _tokenHash,
            issuedAt: _issuedAt,
            expiresAt: _expiresAt,
            consumedAt: _consumedAt,
            consumedByUserId: _consumedByUserId);
}
