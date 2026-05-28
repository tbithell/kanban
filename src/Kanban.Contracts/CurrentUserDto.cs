namespace Kanban.Contracts;

/// <summary>The currently authenticated registered user.</summary>
public sealed class CurrentUserDto
{
    /// <summary>Unique identifier for the user.</summary>
    public required Guid Id { get; init; }

    /// <summary>Email address associated with the user's Google account.</summary>
    public required string Email { get; init; }

    /// <summary>Display name from the user's Google account.</summary>
    public required string DisplayName { get; init; }

    /// <summary>System role: "admin" or "standard".</summary>
    public required string SystemRole { get; init; }

    /// <summary>UTC timestamp when the user was first registered.</summary>
    public required DateTimeOffset RegisteredAt { get; init; }

    /// <summary>UTC timestamp of the user's most recent sign-in. Null for first-time registration response.</summary>
    public DateTimeOffset? LastSignInAt { get; init; }
}
