# Implementation Plan: Authentication and User Onboarding

**Branch**: `001-auth-onboarding` | **Date**: 2026-05-25 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-auth-onboarding/spec.md`

## Summary

Establishes the identity foundation for the entire application: seeding the initial admin user,
gating all API access via Google OAuth + RegisteredUser policy, and enabling admins to issue
single-use time-limited invitation tokens that prospective users accept to become registered.
No email delivery — the admin copies the redemption link from the UI response and shares it
out-of-band.

Technical approach: ASP.NET 10 OIDC middleware handles Google authentication; a custom
`RegisteredUserRequirement` enforces the invite-only gate; a `OnTicketReceived` callback links
Google identity to the seeded admin record on first sign-in and adds the `system_role` claim for
all subsequent requests without a DB hit; invitation tokens are 256-bit random values hashed with
SHA-256 before storage; concurrent acceptance safety is achieved via a single UPDATE with a
nullity check on `consumed_at` inside a deferred SQLite transaction.

## Technical Context

**Language/Version**: C# (.NET 10), TypeScript (React latest stable / Vite)

**Primary Dependencies**:
- Backend: ASP.NET 10 minimal API, `Microsoft.AspNetCore.Authentication.Google` (OIDC),
  Dapper, DbUp, FluentValidation, Polly, `Microsoft.Extensions.Http.Resilience`,
  `NetEscapades.AspNetCore.SecurityHeaders`, `Asp.Versioning.Http`,
  `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`,
  `Microsoft.Extensions.Diagnostics.HealthChecks`
- Frontend: React (latest), Vite, TypeScript, `@fluentui/react-components`,
  `@tanstack/react-query`, `@tanstack/react-query-devtools`, `react-error-boundary`

**Storage**: SQLite (local dev + CI); `IDbConnection` abstraction preserves Postgres swap

**Testing**: xUnit + FluentAssertions (unit + integration), React Testing Library + Jest
(component), Playwright (e2e)

**Target Platform**: Local development machine — web app, no production deploy required

**Project Type**: Web application — REST API (ASP.NET 10) + SPA (React/Vite)

**Performance Goals**: First admin sign-in < 60 s end-to-end (SC-001); invite issuance < 10 s
(SC-003); invitee acceptance < 2 min (SC-004); subsequent sign-in < 5 s (SC-008);
API responses < 200 ms p95

**Constraints**: Local demo only; no SMTP; no external infrastructure beyond Google OAuth

**Scale/Scope**: MVP — one seeded admin, small number of invited users; single-replica SQLite

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Spec-First (I) | ✅ PASS | `spec.md` complete; all 16 quality checklist items passed |
| TDD mandatory (II) | ✅ PASS | All four test layers planned; red-before-green enforced in task order |
| Simplicity (III) | ✅ PASS | No abstractions beyond spec requirements |
| Security-First (IV) | ✅ PASS | Google OAuth only; gitleaks hook required before first commit; Snyk extension required |
| SOLID (V) | ✅ PASS | One class per responsibility; DI throughout |
| Self-Documenting (VI) | ✅ PASS | No in-class comments; names express intent |
| Design Patterns (VII) | ✅ PASS | Repository, Adapter, Factory, Builder per layer rules |
| MVP-Oriented (VIII) | ✅ PASS | All three P1 stories = minimum viable onboarding loop |
| NFRs (IX) | ✅ PASS | Performance targets from SC-001–SC-008; WCAG AA on all UI |
| Entities never cross API boundary | ✅ PASS | DTOs in `Kanban.Contracts`; transforms in `Kanban.Business` |
| RegisteredUser policy gate | ✅ PASS | All endpoints except `/invites/{token}/accept` require RegisteredUser |
| Verify class in non-API public methods | ✅ PASS | Required in Business, DataAccess, Domain, AntiCorruption |
| gitleaks pre-commit hook | ✅ PASS | Must be installed before first commit — hard gate |
| DDD aggregate roots | ✅ PASS | `User` and `Invitation` are aggregate roots |
| IDbConnection only in DataAccess | ✅ PASS | No concrete connection types outside DI registration |
| Deferred transactions + Polly | ✅ PASS | Concurrent acceptance uses deferred txn; Polly wraps retry |
| MVP scoping (Implementation Order) | ✅ PASS | All referenced sections are category (a) "implement now" |

Post-Phase-1 re-check: see bottom of this file.

## Project Structure

### Documentation (this feature)

```text
specs/001-auth-onboarding/
├── plan.md              ← this file
├── research.md          ← Phase 0: token strategy, OIDC pattern, concurrent acceptance
├── data-model.md        ← Phase 1: DB schema, entities, migrations
├── quickstart.md        ← Phase 1: dev setup for this feature
├── contracts/
│   ├── endpoints.md     ← Phase 1: API endpoint contracts
│   └── dtos.md          ← Phase 1: DTO shapes
└── tasks.md             ← Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code

```text
src/
├── Kanban.Api/
│   ├── Program.cs                         # OIDC setup, policy registration, middleware order,
│   │                                      #   endpoint groups, health checks, options validation
│   ├── Options/
│   │   ├── GoogleAuthOptions.cs
│   │   ├── SeedOptions.cs
│   │   ├── CorsOptions.cs
│   │   └── ConnectionStringOptions.cs
│   ├── Auth/
│   │   ├── RegisteredUserRequirement.cs
│   │   ├── RegisteredUserHandler.cs
│   │   └── CurrentUserService.cs          # ICurrentUserService — resolves User.Id from sub claim
│   └── Endpoints/
│       ├── AuthEndpoints.cs               # GET /api/v1/auth/me, GET /api/v1/auth/signin,
│       │                                  #   POST /api/v1/auth/signout
│       └── InviteEndpoints.cs             # POST /api/v1/invites,
│                                          #   POST /api/v1/invites/{token}/accept
│
├── Kanban.Web/
│   └── src/
│       ├── pages/
│       │   ├── SignInPage.tsx
│       │   ├── NotRegisteredPage.tsx
│       │   └── AcceptInvitePage.tsx       # /accept/:token
│       ├── components/
│       │   └── admin/
│       │       └── InviteUserDialog.tsx
│       └── hooks/
│           ├── useCurrentUser.ts
│           └── useIssueInvite.ts
│
├── Kanban.Business/
│   ├── Services/
│   │   ├── InvitationService.cs
│   │   └── AuthService.cs                 # OnTicketReceived logic — admin linking, claim injection
│   └── Interfaces/
│       ├── IInvitationService.cs
│       └── IAuthService.cs
│
├── Kanban.Domain/
│   ├── Entities/
│   │   ├── User.cs                        # Aggregate root
│   │   └── Invitation.cs                  # Aggregate root
│   ├── ValueObjects/
│   │   └── InvitationToken.cs             # Generation + SHA-256 hashing
│   ├── Events/
│   │   └── AuthEvent.cs
│   ├── Enums/
│   │   ├── SystemRole.cs
│   │   └── AuthEventType.cs
│   └── Verify.cs                          # Fluent parameter verification
│
├── Kanban.Contracts/
│   ├── IssueInviteRequest.cs
│   ├── IssueInviteResponse.cs
│   └── CurrentUserDto.cs
│
├── Kanban.AntiCorruption/
│   └── Adapters/
│       └── GoogleIdentityAdapter.cs       # Extracts sub + email from ClaimsPrincipal
│
├── Kanban.Data/
│   └── migrations/
│       ├── sqlite/
│       │   ├── 001_initial_schema.sql
│       │   └── 002_seed_admin.sql
│       └── postgres/
│           ├── 001_initial_schema.sql
│           └── 002_seed_admin.sql
│
└── Kanban.DataAccess/
    ├── Interfaces/
    │   ├── IUserRepository.cs
    │   ├── IInvitationRepository.cs
    │   └── IAuthEventRepository.cs
    └── Repositories/
        ├── UserRepository.cs
        ├── InvitationRepository.cs
        └── AuthEventRepository.cs

tests/
├── unit/
│   ├── Builders/
│   │   ├── UserBuilder.cs
│   │   └── InvitationBuilder.cs
│   └── Business/
│       ├── InvitationServiceTests.cs      # 12 unit tests covering all FR-006–FR-013
│       └── AuthServiceTests.cs            # 6 unit tests covering FR-004, FR-005, FR-019
│
├── integration/
│   └── Api/
│       ├── AuthEndpointTests.cs           # /auth/me (registered + unregistered), /auth/signout
│       └── InviteEndpointTests.cs         # issue + accept (all scenarios), concurrent acceptance
│
└── e2e/
    └── Auth/
        ├── AdminSignInTests.cs            # US1 acceptance scenarios 1–4
        └── InviteAcceptanceTests.cs       # US2 scenarios 1–5, US3 scenarios 1–6

src/Kanban.Web/tests/
└── components/
    ├── SignInPage.test.tsx
    ├── NotRegisteredPage.test.tsx
    ├── AcceptInvitePage.test.tsx
    └── InviteUserDialog.test.tsx
```

**Structure Decision**: Web application layout. Backend is the eight-project structure mandated
by the constitution. Frontend is Vite/React in `src/Kanban.Web/`. Tests follow the three-folder
convention (`tests/unit/`, `tests/integration/`, `tests/e2e/`) plus `src/Kanban.Web/tests/`
for RTL component tests.

## Post-Phase-1 Constitution Re-check

| Gate | Status | Notes |
|------|--------|-------|
| All Phase 0 gates | ✅ PASS | Unchanged |
| Data model cross-boundary check | ✅ PASS | DB entities mapped to domain objects in DataAccess; DTOs in Contracts |
| No `RETURNING` in INSERT | ✅ PASS | Insert + separate SELECT pattern used throughout |
| Concurrent acceptance safety | ✅ PASS | Single deferred-txn UPDATE with `consumed_at IS NULL` guard; Polly retry on lock |
| Token storage — non-replayable | ✅ PASS | SHA-256 hash stored; raw token returned to admin once, never persisted |
| Audit records — no PII | ✅ PASS | AuthEvent stores only GUIDs, event type, outcome — no email, token, name |
| RegisteredUser policy — 403 not 401 | ✅ PASS | Handler returns `Forbid()` (403), not `Challenge()` (401) |
| Accept endpoint auth scope | ✅ PASS | Uses `[Authorize]` only (Google auth), not RegisteredUser — invitee is not yet registered |

## Complexity Tracking

No constitution violations requiring justification.
