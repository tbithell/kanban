# DTOs: Authentication and User Onboarding

All types live in `Kanban.Contracts`. All properties have XML doc comments (auto-populates
OpenAPI schema descriptions). All classes are `sealed` with `init`-only setters.

---

## CurrentUserDto

Returned by `GET /api/v1/auth/me` and `POST /api/v1/invites/{token}/accept`.

```csharp
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
```

---

## IssueInviteRequest

Request body for `POST /api/v1/invites`.

```csharp
/// <summary>Request to issue an invitation to a new user.</summary>
public sealed class IssueInviteRequest
{
    /// <summary>Email address to invite. Must be a valid email format and not already registered.</summary>
    public required string Email { get; init; }
}
```

**FluentValidation** (`IssueInviteRequestValidator` in `Kanban.Api`):
```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress()
    .WithMessage("Must be a valid email address.");
```

---

## IssueInviteResponse

Response body for `POST /api/v1/invites` (both 201 new and 200 existing).

```csharp
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
```

---

## Entity → DTO Mapping (Kanban.Business)

Transforms live exclusively in `Kanban.Business`. No mapping libraries — simple property
assignment.

```csharp
// UserTransforms.cs
public static CurrentUserDto ToDto(User user) => new()
{
    Id           = user.Id,
    Email        = user.Email,
    DisplayName  = user.DisplayName,
    SystemRole   = user.SystemRole.ToString().ToLower(),
    RegisteredAt = user.RegisteredAt,
    LastSignInAt = user.LastSignInAt
};

// InvitationTransforms.cs
public static IssueInviteResponse ToResponse(Invitation invitation, string rawToken,
                                              string frontendBaseUrl) => new()
{
    Token          = rawToken,
    RedemptionLink = $"{frontendBaseUrl}/accept/{rawToken}",
    ExpiresAt      = invitation.ExpiresAt
};
```

`frontendBaseUrl` is resolved from `IOptions<CorsOptions>.AllowedOrigins[0]` — the single
frontend origin for MVP.
