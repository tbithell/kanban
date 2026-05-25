# Research: Authentication and User Onboarding

**Phase**: 0 | **Branch**: `001-auth-onboarding`

## Decision 1: Invitation Token Generation and Storage

**Decision**: 256-bit random token (URL-safe base64, no padding) hashed with SHA-256 before
storage. Raw token returned to admin once at issuance; never stored.

**Rationale**:
- `RandomNumberGenerator.GetBytes(32)` → `Convert.ToBase64String(...).TrimEnd('=').Replace('+','-').Replace('/','_')` → 43-char URL-safe string
- SHA-256 of a 256-bit random token is preimage-resistant: recovering the raw token from the
  hash is computationally infeasible even with full DB access
- Satisfies FR-009: "cryptographically unguessable" + "stored in a form that cannot be replayed
  if the storage is compromised"
- Avoids HMAC: no key management overhead for MVP; SHA-256 of high-entropy input is sufficient

**Alternatives considered**:
- HMAC-SHA256 with server key: better security posture but adds key rotation complexity; overkill
  for a 256-bit random token
- bcrypt/Argon2: designed for slow password hashing; unnecessary overhead for high-entropy tokens
  that have no brute-force attack surface

**How it flows**:
```
Issue:  rawToken = base64url(RandomNumberGenerator.GetBytes(32))
        hash     = hex(SHA256(UTF8(rawToken)))
        DB stores: hash
        Response: rawToken → admin copies, shares out-of-band

Accept: rawToken arrives in URL path segment
        hash = hex(SHA256(UTF8(rawToken)))
        DB lookup: SELECT * FROM invitations WHERE token_hash = @hash
```

---

## Decision 2: OIDC Integration Pattern — OnTicketReceived Callback

**Decision**: All authentication-related logic runs in the `OnTicketReceived` callback, which
adds `user_id` and `system_role` claims to the auth session. The `RegisteredUserHandler` checks
for the presence of `user_id` claim — no DB hit on subsequent requests for registered users.

**Rationale**: Constitution mandates `system_role` claim stored in auth session, no DB lookup per
request. Claim absence is the signal for "not registered" — efficient and correct.

**Admin first sign-in flow**:
```
OnTicketReceived:
  sub   = ctx.Principal.FindFirst("sub")
  email = ctx.Principal.FindFirst("email")

  user = userRepo.FindByGoogleSub(sub)          ← returning user fast path
  if user == null:
    user = userRepo.FindByEmail(email)           ← seeded admin (no sub yet)
    if user != null && user.GoogleSub == null:
      userRepo.LinkGoogleSub(user.Id, sub)       ← permanent link, first sign-in only
  
  if user == null:
    → do NOT add user_id/system_role claims
    → user is authenticated by Google but not registered
    → RegisteredUserHandler will return 403 on any RegisteredUser endpoint
    return (no redirect in server — frontend handles 403 response)
  
  identity.AddClaim("user_id", user.Id)
  identity.AddClaim("system_role", user.SystemRole.ToLower())
  userRepo.UpdateLastSignIn(user.Id, UtcNow)
  authEventRepo.Record(SignIn, user.Id, "Success")
```

**Unregistered user flow** (general sign-in, no invite):
- Google auth succeeds → OIDC issues cookie without `user_id`/`system_role` claims
- Any request to a RegisteredUser endpoint → `RegisteredUserHandler.HandleRequirementAsync`
  checks for `user_id` claim → absent → `context.Fail()` → 403 with `code: "user.not_registered"`
- Frontend receives 403 → shows `NotRegisteredPage`

**Why no redirect in `OnTicketReceived`**:
- Redirecting from `OnTicketReceived` is fragile: it short-circuits the OIDC middleware and
  requires careful state management
- The SPA-friendly approach is: OIDC issues the cookie regardless, then the frontend/API handles
  the authorization check via the policy — clean separation

---

## Decision 3: Invitation Acceptance — Frontend-Driven Flow

**Decision**: Redemption link points to the SPA (`http://localhost:5173/accept/{token}`), not the
API. The SPA page stores the token in sessionStorage, triggers Google sign-in, then POSTs to
`POST /api/v1/invites/{token}/accept` after authentication completes.

**Flow**:
```
1. Admin receives: { token: "abc123", redemptionLink: "http://localhost:5173/accept/abc123" }
2. Invitee opens the SPA route /accept/abc123
3. SPA: AcceptInvitePage reads :token param, stores in sessionStorage["pendingInviteToken"]
4. SPA: user clicks "Accept & Sign in with Google"
5. SPA: window.location.href = "/api/v1/auth/signin?returnUrl=/accept/callback"
6. API: GET /api/v1/auth/signin → Challenge(Google, returnUrl)
7. Google OAuth completes → OnTicketReceived (invitee is not registered → no user_id claim)
8. OIDC redirects back to /accept/callback (SPA route)
9. SPA /accept/callback: reads sessionStorage["pendingInviteToken"], POSTs to
   POST /api/v1/invites/{token}/accept
10. API: validates token, creates user, refreshes auth cookie with user_id + system_role claims
11. SPA: redirects to dashboard (user is now registered, claims are in the refreshed cookie)
```

**Auth cookie refresh after acceptance** (step 10):
```csharp
// After user record created in accept endpoint:
var identity = (ClaimsIdentity)HttpContext.User.Identity!;
identity.AddClaim(new Claim("user_id", newUser.Id.ToString()));
identity.AddClaim(new Claim("system_role", "standard"));
await HttpContext.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme,
    new ClaimsPrincipal(identity));
```
This refreshes the auth cookie in-place — no second Google OAuth round-trip required.

**Rationale**: Keeps the SPA as the routing authority. Backend remains a pure API — no server-side
redirect orchestration. Token is in sessionStorage (not the URL) after the Google redirect returns,
avoiding token leakage in HTTP Referrer headers during the OAuth round-trip.

---

## Decision 4: Concurrent Acceptance Safety

**Decision**: Single `UPDATE invitations SET consumed_at = @now, consumed_by_user_id = @userId
WHERE token_hash = @hash AND consumed_at IS NULL AND expires_at > @now` inside a deferred
SQLite transaction, with Polly retry on lock contention.

**Rationale**:
- SQLite serializes write transactions — at most one `UPDATE` modifies the row at a time
- The `consumed_at IS NULL` predicate makes the UPDATE idempotent: the second concurrent
  request will find 0 rows affected (consumed_at is already set) and receive the "already
  consumed" refusal
- No savepoints needed here (no read-then-check-then-write pattern; the WHERE clause IS the
  check)
- Polly wraps the retry on `SqliteException` with reason "database is locked" (SQLITE_BUSY)

**Pseudocode**:
```
BEGIN DEFERRED TRANSACTION
  rowsAffected = UPDATE invitations
                 SET consumed_at = @now, consumed_by_user_id = @userId
                 WHERE token_hash = @hash
                   AND consumed_at IS NULL
                   AND expires_at > @now

  if rowsAffected == 0:
    ROLLBACK
    throw AlreadyConsumedOrInvalidException()  // same message for all refusal reasons (FR-016)

  INSERT INTO users (...) VALUES (...)
  COMMIT
```

---

## Decision 5: Admin Seeding via DbUp Migration

**Decision**: DbUp seed migration `002_seed_admin.sql` inserts the admin user record using the
`Seed:AdminEmail` configuration value. The migration is parameterized at runtime — DbUp executes
it with the email injected from `IOptions<SeedOptions>` before DbUp runs. The `google_sub` column
is left NULL; it is populated on the admin's first Google sign-in (Decision 2).

**Seed migration approach**:
DbUp `SqlScriptOptions` does not natively support parameterized scripts. Instead, the DbUp setup
code reads `SeedOptions.AdminEmail` and passes it as a script variable replacement:
```csharp
DeployChanges.To.SQLiteDatabase(connectionString)
    .WithScriptsFromEmbeddedResources(...)
    .WithVariable("AdminEmail", seedOptions.AdminEmail)
    .LogToConsole()
    .Build()
    .PerformUpgrade();
```
SQL: `INSERT INTO users (..., email, ...) VALUES (..., '$AdminEmail$', ...)`

If the admin record already exists (re-run after a fresh DB), DbUp skips the migration (it's
already in the `SchemaVersions` journal).

---

## Decision 6: ICurrentUserService — Identity Seam

**Decision**: Scoped `ICurrentUserService` resolves the authenticated user's ID and role from
claims via `IHttpContextAccessor`. Injected into Business layer services via constructor DI.

**Rationale**: Constitution mandates this seam as the insertion point for future `TenantContext`.
Business layer never touches `IHttpContextAccessor` directly — only `ICurrentUserService`.

```csharp
public interface ICurrentUserService
{
    Guid UserId { get; }
    SystemRole SystemRole { get; }
    bool IsAuthenticated { get; }
}
```

---

## Dependencies Confirmed

All dependencies are in the (a) "implement now" category per the MVP Implementation Order:

| Package | Purpose | Notes |
|---------|---------|-------|
| `Microsoft.AspNetCore.Authentication.Google` | OIDC + Google OAuth | Built into ASP.NET OIDC |
| `Dapper` | SQL mapping | Via `IDbConnection` |
| `DbUp-SQLite` | Migrations + seeding | `WithVariable` for AdminEmail |
| `FluentValidation.AspNetCore` | DTO validation at API boundary | InviteRequest.Email |
| `Polly` | Retry on SQLite lock contention | Standard resilience pattern |
| `Microsoft.Extensions.Http.Resilience` | HTTP resilience on Google OIDC adapter | `AddStandardResilienceHandler` |
| `NetEscapades.AspNetCore.SecurityHeaders` | HSTS, CSP, security headers | Cheap hygiene |
| `Asp.Versioning.Http` | /api/v1/ routing group | URL path versioning |
| `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` | Dev-only API docs | IsDevelopment guard |
