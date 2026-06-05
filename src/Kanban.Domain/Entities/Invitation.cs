using Kanban.Domain.Enums;

namespace Kanban.Domain.Entities;

public sealed class Invitation
{
    public Guid Id { get; }
    public string Email { get; }
    public Guid IssuedByUserId { get; }
    public string TokenHash { get; }
    public DateTimeOffset IssuedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public Guid? ConsumedByUserId { get; private set; }
    public Guid? BoardId { get; init; }
    public BoardRole? BoardRole { get; init; }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsConsumed => ConsumedAt.HasValue;
    public bool IsRedeemable => !IsExpired && !IsConsumed;

    public Invitation(Guid id, string email, Guid issuedByUserId, string tokenHash,
                      DateTimeOffset issuedAt, DateTimeOffset expiresAt,
                      DateTimeOffset? consumedAt, Guid? consumedByUserId)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(email).IsNotNull().IsNotEmpty();
        Verify.That(issuedByUserId).IsNotDefault();
        Verify.That(tokenHash).IsNotNull().IsNotEmpty();

        Id = id;
        Email = email;
        IssuedByUserId = issuedByUserId;
        TokenHash = tokenHash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        ConsumedAt = consumedAt;
        ConsumedByUserId = consumedByUserId;
    }

    public bool EmailMatches(string googleEmail)
    {
        Verify.That(googleEmail).IsNotNull().IsNotEmpty();
        return string.Equals(Email, googleEmail, StringComparison.OrdinalIgnoreCase);
    }

    public void Consume(Guid userId, DateTimeOffset consumedAt)
    {
        Verify.That(userId).IsNotDefault();
        ConsumedAt = consumedAt;
        ConsumedByUserId = userId;
    }
}
