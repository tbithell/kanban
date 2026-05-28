namespace Kanban.Contracts;

/// <summary>Request to issue an invitation to a new user.</summary>
public sealed class IssueInviteRequest
{
    /// <summary>Email address to invite. Must be a valid email format and not already registered.</summary>
    public required string Email { get; init; }
}
