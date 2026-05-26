namespace Kanban.Contracts;

/// <summary>Result of a successful invitation issuance.</summary>
public sealed class IssueInviteResponse
{
    /// <summary>
    /// The raw invitation token. Share this URL with the invitee — it is shown once and not stored.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>Full redemption URL the admin should share with the invitee.</summary>
    public required string RedemptionLink { get; init; }

    /// <summary>UTC timestamp after which this invitation expires and cannot be redeemed.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
