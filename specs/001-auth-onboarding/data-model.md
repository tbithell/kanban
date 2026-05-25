# Data Model: Authentication and User Onboarding

**Phase**: 1 | **Branch**: `001-auth-onboarding`

## Domain Entities

### User (Aggregate Root)

Represents a person who can sign in. Administrators and standard users are distinguished by
`SystemRole`. The Google subject identifier (`google_sub`) is the stable identity — not the
email — so that a Google email change does not break sign-in.

```csharp
public sealed class User
{
    public Guid Id { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public SystemRole SystemRole { get; }
    public string? GoogleSub { get; private set; }      // null until first sign-in
    public DateTimeOffset RegisteredAt { get; }
    public DateTimeOffset? LastSignInAt { get; private set; }

    public User(Guid id, string email, string displayName, SystemRole systemRole,
                string? googleSub, DateTimeOffset registeredAt, DateTimeOffset? lastSignInAt)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(email).IsNotNull().IsNotEmpty();
        Verify.That(displayName).IsNotNull().IsNotEmpty();
        RegisteredAt = registeredAt;
        // ... assign all fields
    }

    public void LinkGoogleIdentity(string googleSub)
    {
        Verify.That(googleSub).IsNotNull().IsNotEmpty();
        GoogleSub = googleSub;
    }

    public void RecordSignIn(DateTimeOffset signedInAt) => LastSignInAt = signedInAt;
}
```

### Invitation (Aggregate Root)

Represents a pending or consumed offer for a person to become a registered user. The raw
redemption token is never stored — only its SHA-256 hash.

```csharp
public sealed class Invitation
{
    public Guid Id { get; }
    public string Email { get; }                        // email the invitation was issued to
    public Guid IssuedByUserId { get; }
    public string TokenHash { get; }                    // SHA-256 hex of raw token
    public DateTimeOffset IssuedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public Guid? ConsumedByUserId { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsConsumed => ConsumedAt.HasValue;
    public bool IsRedeemable => !IsExpired && !IsConsumed;

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
```

### InvitationToken (Value Object)

Encapsulates token generation and hashing. Ensures the raw token never leaks into storage.

```csharp
public sealed record InvitationToken
{
    public string Raw { get; }        // returned to admin, never stored
    public string Hash { get; }       // SHA-256 hex — stored in DB

    private InvitationToken(string raw, string hash) { Raw = raw; Hash = hash; }

    public static InvitationToken Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLower();
        return new InvitationToken(raw, hash);
    }

    public static string HashRaw(string rawToken)
    {
        Verify.That(rawToken).IsNotNull().IsNotEmpty();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLower();
    }
}
```

### AuthEvent (Audit Record)

Immutable record of a notable auth lifecycle moment. No PII — only GUIDs and outcome codes.

```csharp
public sealed record AuthEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    AuthEventType EventType,
    Guid? UserId,           // nullable: e.g., acceptance refused for unknown token
    string Outcome          // "Success", "NotRegistered", "TokenExpired", etc.
);
```

### Enumerations

```csharp
public enum SystemRole  { Admin, Standard }
public enum AuthEventType { SignIn, SignOut, InvitationIssued, InvitationAccepted, AcceptanceRefused }
```

---

## Database Schema

### SQLite Migration: `001_initial_schema.sql`

```sql
CREATE TABLE IF NOT EXISTS users (
    id              TEXT NOT NULL PRIMARY KEY,
    email           TEXT NOT NULL,
    display_name    TEXT NOT NULL,
    system_role     TEXT NOT NULL DEFAULT 'Standard',
    google_sub      TEXT,
    registered_at   TEXT NOT NULL,
    last_sign_in_at TEXT,
    CONSTRAINT uq_users_email      UNIQUE (email),
    CONSTRAINT uq_users_google_sub UNIQUE (google_sub)
);

CREATE TABLE IF NOT EXISTS invitations (
    id                   TEXT NOT NULL PRIMARY KEY,
    email                TEXT NOT NULL,
    issued_by_user_id    TEXT NOT NULL REFERENCES users(id),
    token_hash           TEXT NOT NULL,
    issued_at            TEXT NOT NULL,
    expires_at           TEXT NOT NULL,
    consumed_at          TEXT,
    consumed_by_user_id  TEXT REFERENCES users(id),
    CONSTRAINT uq_invitations_token_hash UNIQUE (token_hash)
);

CREATE INDEX IF NOT EXISTS ix_invitations_email      ON invitations(email);
CREATE INDEX IF NOT EXISTS ix_invitations_token_hash ON invitations(token_hash);

CREATE TABLE IF NOT EXISTS auth_events (
    id           TEXT NOT NULL PRIMARY KEY,
    occurred_at  TEXT NOT NULL,
    event_type   TEXT NOT NULL,
    user_id      TEXT REFERENCES users(id),
    outcome      TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_auth_events_occurred_at ON auth_events(occurred_at);
```

### SQLite Migration: `002_seed_admin.sql`

```sql
INSERT OR IGNORE INTO users (id, email, display_name, system_role, google_sub, registered_at)
VALUES (
    '$AdminUserId$',
    '$AdminEmail$',
    'Administrator',
    'Admin',
    NULL,
    '$SeedTimestamp$'
);
```

Variables injected by DbUp at startup from `IOptions<SeedOptions>` and `IOptions<SeedMetaOptions>`.
`google_sub` is NULL — populated on first Google sign-in.

### PostgreSQL Variants (`migrations/postgres/`)

Identical DDL except:
- `TEXT` primary keys remain `TEXT` (Postgres supports this; GUID stored as text for portability)
- `CREATE INDEX IF NOT EXISTS` syntax is identical in Postgres 9.5+
- `INSERT OR IGNORE` → `INSERT INTO ... ON CONFLICT DO NOTHING`

---

## Repository Interfaces

```csharp
// Kanban.DataAccess/Interfaces/IUserRepository.cs
public interface IUserRepository
{
    Task<User?> FindByGoogleSubAsync(string googleSub, IDbTransaction? tx = null);
    Task<User?> FindByEmailAsync(string email, IDbTransaction? tx = null);
    Task<User?> FindByIdAsync(Guid id, IDbTransaction? tx = null);
    Task InsertAsync(User user, IDbTransaction tx);
    Task LinkGoogleSubAsync(Guid userId, string googleSub, IDbTransaction tx);
    Task UpdateLastSignInAsync(Guid userId, DateTimeOffset signedInAt, IDbTransaction tx);
}

// Kanban.DataAccess/Interfaces/IInvitationRepository.cs
public interface IInvitationRepository
{
    Task<Invitation?> FindByTokenHashAsync(string tokenHash, IDbTransaction? tx = null);
    Task<Invitation?> FindActiveByEmailAsync(string email, IDbTransaction? tx = null);
    Task InsertAsync(Invitation invitation, IDbTransaction tx);
    // Consume is handled via a targeted UPDATE (see concurrent acceptance pattern)
    Task<bool> TryConsumeAsync(string tokenHash, Guid userId, DateTimeOffset consumedAt,
                               IDbTransaction tx);
}

// Kanban.DataAccess/Interfaces/IAuthEventRepository.cs
public interface IAuthEventRepository
{
    Task RecordAsync(AuthEvent authEvent, IDbTransaction tx);
}
```

`TryConsumeAsync` executes:
```sql
UPDATE invitations
SET consumed_at = @consumedAt, consumed_by_user_id = @userId
WHERE token_hash = @tokenHash
  AND consumed_at IS NULL
  AND expires_at > @now
```
Returns `true` if `rowsAffected == 1`, `false` otherwise.
