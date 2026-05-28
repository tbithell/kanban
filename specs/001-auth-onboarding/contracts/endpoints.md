# API Endpoint Contracts: Authentication and User Onboarding

**Base URL**: `/api/v1`  
**Auth scheme**: ASP.NET cookie (set by OIDC middleware after Google sign-in)  
**Error format**: RFC 7807 Problem Details — `{ title, status, code, traceId }`

---

## Auth Endpoints

### GET /api/v1/auth/signin

Initiates the Google OAuth challenge. Not versioned as a RegisteredUser endpoint — anyone
can hit it.

**Query params**: `returnUrl` (optional, URL-encoded) — where to redirect after successful sign-in

**Authorization**: None required (triggers sign-in)

**Behavior**: Returns `ChallengeResult(GoogleDefaults.AuthenticationScheme)` with the
`returnUrl` as the `RedirectUri` property. The OIDC middleware handles the Google redirect.

**Success**: 302 → Google OAuth, then back to `returnUrl` (or `/`)

---

### GET /api/v1/auth/me

Returns the currently authenticated registered user.

**Authorization**: `RegisteredUser` policy

**Response 200**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "user@example.com",
  "displayName": "Jane Smith",
  "systemRole": "standard",
  "registeredAt": "2026-05-25T10:00:00Z",
  "lastSignInAt": "2026-05-25T14:30:00Z"
}
```

**Response 403** — `user.not_registered`:
```json
{
  "title": "You are not registered to use this application.",
  "status": 403,
  "code": "user.not_registered",
  "traceId": "..."
}
```

**Response 401**: Unauthenticated — redirected to `/api/v1/auth/signin`

---

### POST /api/v1/auth/signout

Ends the current user's session.

**Authorization**: `Authorize` (any authenticated user — registered or not)

**Response 204**: Session ended. Cookie cleared.

---

## Invite Endpoints

### POST /api/v1/invites

Issues an invitation for a new user. Admin only.

**Authorization**: `Admin` policy

**Request body**:
```json
{ "email": "invitee@example.com" }
```

**Validation** (FluentValidation, returns 422 on failure):
- `email` must be a valid email format (RFC 5322)
- `email` must not correspond to an existing registered user (FR-008)

**Response 201** — new invitation issued:
```json
{
  "token": "aB3cD4eF5gH6iJ7kL8mN9oP0qR1sT2uV",
  "redemptionLink": "http://localhost:5173/accept/aB3cD4eF5gH6iJ7kL8mN9oP0qR1sT2uV",
  "expiresAt": "2026-06-01T10:00:00Z"
}
```

**Response 200** — existing unconsumed unexpired invitation returned (FR-011):
Same body as 201. Admin gets the existing redemption link without creating a duplicate.

**Response 409** — `invite.already_registered`:
```json
{
  "title": "This email address is already registered.",
  "status": 409,
  "code": "invite.already_registered",
  "traceId": "..."
}
```

**Response 422** — validation failure:
```json
{
  "title": "Validation failed.",
  "status": 422,
  "code": "validation.failed",
  "errors": { "email": ["Must be a valid email address."] },
  "traceId": "..."
}
```

**Response 403** — non-admin attempt: standard 403 Problem Details

---

### POST /api/v1/invites/{token}/accept

Accepts an invitation and creates a registered user account. Called by an already
Google-authenticated invitee (not yet a registered user).

**Authorization**: `Authorize` only — Google authentication required; `RegisteredUser` is
explicitly NOT required (invitee has no user record yet)

**Path param**: `token` — the raw invitation token (43-char URL-safe base64)

**Response 200** — accepted, user created, auth cookie refreshed:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "invitee@example.com",
  "displayName": "Jane Smith",
  "systemRole": "standard",
  "registeredAt": "2026-05-25T10:05:00Z"
}
```

**Response 410** — invalid/expired/consumed token (FR-016 — identical for all three cases):
```json
{
  "title": "This invitation is no longer valid. Please request a new one.",
  "status": 410,
  "code": "invite.invalid",
  "traceId": "..."
}
```

**Response 422** — email mismatch (FR-017):
```json
{
  "title": "This invitation was issued to a different email address.",
  "status": 422,
  "code": "invite.email_mismatch",
  "traceId": "..."
}
```

---

## OIDC Callback (Middleware-Handled — Not a Manual Endpoint)

`GET /signin-google` is the OAuth redirect URI registered in Google Cloud Console.
It is handled entirely by `Microsoft.AspNetCore.Authentication.Google` — no endpoint handler
is written.

**Google Cloud Console Authorized Redirect URI**:
- Dev HTTP: `http://localhost:5077/signin-google`
- Dev HTTPS: `https://localhost:7282/signin-google`

---

## Global Headers (All Responses)

| Header | Value |
|--------|-------|
| `X-Correlation-Id` | Request correlation ID (echoed from inbound or generated) |
| `api-supported-versions` | `1.0` |
| Security headers | Per `NetEscapades.AspNetCore.SecurityHeaders` setup (HSTS, CSP, etc.) |

---

## Middleware Order

```
UseForwardedHeaders
UseHttpsRedirection (production only)
UseSecurityHeaders
UseRouting
UseCors("KanbanWebApp")
UseRateLimiter
UseAuthentication
UseAuthorization
```
