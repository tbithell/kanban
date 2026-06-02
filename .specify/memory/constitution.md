<!--
SYNC IMPACT REPORT
Version: 1.7.1 → 1.7.2 (PATCH — Clarify Vitest as frontend test runner)
Modified Principles: none
Added Sections: none
Modified Sections:
  - Principle II TDD — Component test layer now says Vitest (not Jest); Vite projects
    use Vitest as the idiomatic Jest-compatible runner. All RTL APIs are unchanged.
  - Stack & Constraints table — Frontend component tests updated to Vitest
  - CI/CD pipeline shape updated to reflect Vitest
Removed Sections: none
Stack Table: Frontend component tests: React Testing Library + Vitest
Templates: no changes required
Deferred TODOs: none
-->

# Kanban Constitution

## Core Principles

### I. Spec-First
All features MUST begin with a written specification produced via `/speckit-specify` before any
code is written. Specifications drive implementation — not the reverse. Specs MUST be committed
before an implementation branch is opened.

### II. Test-Driven Development (NON-NEGOTIABLE)
TDD is mandatory for all implementation work, including AI agent sessions.

- Tests MUST be written before implementation code.
- Tests MUST fail (red) before implementation begins.
- Implementation proceeds until tests pass (green), then refactor.
- The Red-Green-Refactor cycle MUST be verifiable in commit history.

**Backend test stack: xUnit + FluentAssertions.** FluentAssertions MUST be used for all
xUnit assertions — `result.Should().Be(expected)` not `Assert.Equal`.

Four test layers are required on every feature:

| Layer | Tool | Location | Covers |
|-------|------|----------|--------|
| Unit | xUnit + FluentAssertions | `tests/unit/` | Business logic, domain, services in isolation |
| Integration | xUnit + FluentAssertions | `tests/integration/` | REST endpoints + real DB (SQLite locally, Postgres via Testcontainers in CI) |
| Component | React Testing Library + Vitest | `src/Kanban.Web/tests/` | Fluent UI component rendering, user interactions |
| E2E | Playwright | `tests/e2e/` | All user scenarios from the feature spec |

**Integration test database engines:** Local developer runs use SQLite for fast
feedback. CI MUST run integration tests against both SQLite AND Postgres
(Testcontainers) — see *Integration Tests — Testcontainers in CI*.

**Fluent UI component test tiers** (decision tree — use the lightest sufficient tier):

1. Logic/utility functions testable in isolation → Vitest unit test
2. Requires component rendering without a browser → React Testing Library + Vitest
3. Requires actual browser behavior → Playwright e2e

**RTL component contract rules:**

- RTL verifies rendering given state — does the component show the right thing when the
  hook returns a given value? RTL MUST NOT attempt to verify async mutation lifecycle
  (whether `navigate` fires after a real network call). Playwright owns that contract.
- Mutation hook stubs MUST be typed as `UseMutationResult<TData, TError, TVariables>`
  using the `satisfies` operator, not cast with `as` or typed loosely as `vi.fn()`.
  TypeScript then catches API surface changes at compile time — a renamed field or changed
  argument type breaks the stub, not silently at runtime.

```tsx
const idleMutation = {
  mutate: vi.fn(),
  mutateAsync: vi.fn(),
  isPending: false,
  isError: false,
  error: null,
  data: undefined,
  reset: vi.fn(),
  // remaining UseMutationResult fields...
} satisfies UseMutationResult<User, ApiError, string>
```

**Playwright E2E auth setup — persona classification:**

Test personas fall into two categories with different auth paths:

| Persona | Has real Google account? | Google OAuth path | Dev endpoint path |
|---------|--------------------------|-------------------|-------------------|
| Admin | Yes | ✅ `google: authenticate as admin` — manual sign-in, saves `storageState` | ✅ `bypass: authenticate as admin` — CI / no-browser |
| Invitee | No — synthetic email | ❌ Does not exist | ✅ Only path — always runs |
| Unregistered | No — synthetic email | ❌ Does not exist | ✅ Only path — always runs |

Rules:
- Synthetic test personas (invitee, unregistered) MUST NOT have a `setup.skip` bypass
  condition — the dev endpoint is the only auth path for them regardless of mode.
- The real-Google admin setup step MUST include an `fs.existsSync` guard to skip re-auth
  when a saved session already exists. Delete the file to force a fresh sign-in.
- The dev authenticate endpoint (`GET /api/v1/dev/authenticate`) is registered only in
  `IsDevelopment()` and MUST remain `AllowAnonymous`. It MUST NOT ship to production.

Automated test coverage MUST be ≥ 90%. AI agents MUST follow TDD. A task is not complete
until all four test layers pass.

### III. Simplicity at Velocity
Dependencies MUST be justified. The minimum viable dependency set is the right dependency set.

- No abstraction without a proven, present need (YAGNI).
- Prefer framework and standard-library primitives over third-party packages.
- Three similar lines of code are preferable to a premature abstraction.
- Complexity introduced MUST be documented in the plan's Complexity Tracking table.

### IV. Security-First
Security is a constraint on every task, not a phase.

- Authentication MUST use Google OAuth 2.0 via ASP.NET OIDC middleware. No custom credential
  storage is permitted.
- All user input MUST be validated at system boundaries (API layer). Trust nothing from the client.
  Use ASP.NET model validation (`[Required]`, `[MaxLength]`, `FluentValidation`) on every DTO.
- OWASP Top 10 awareness is required for all API and UI work.

**SQL injection prevention (Dapper-specific):**
- ALL Dapper queries MUST use `@paramName` placeholders. String concatenation into SQL is a
  critical violation — even for `ORDER BY` clauses or dynamic filters.
- Dynamic `ORDER BY`: use a compile-time whitelist dictionary and look up the safe column name.
  Never interpolate user-supplied values into SQL text.
- Dynamic `WHERE` filters: build predicates with a parameter dictionary, never via string
  interpolation.

**Secret detection — hard gate (non-negotiable):**
- **gitleaks** MUST be installed as a git pre-commit hook. A commit containing a detected secret
  MUST be blocked at the hook level. No bypass (`--no-verify`) without explicit written approval.
- A `.gitleaks.toml` MUST be committed to the repo root and kept current.
- If gitleaks is not installed, the pre-commit hook MUST refuse and print install instructions.
- Environment variables and secrets managers are the only permitted credential transport.
  Hardcoded secrets anywhere in the codebase — including test fixtures — are a critical violation.

**Snyk (free tier — https://app.snyk.io) MUST cover the full security surface:**
- **SCA**: dependency vulnerability detection
- **SAST**: code-level security issues in C# and TypeScript
- **License compliance**: open source license risk flagged before merge
- **IaC scanning**: infrastructure-as-code files scanned if present
- **Container**: image scanning for base-image CVEs (required when containers are built in CI)
- All developers MUST have the **Snyk VS Code extension** installed and active.
- Snyk-flagged medium, high, or critical severity findings block merge.

**Security headers — defense in depth (non-negotiable):**
- Response security headers (HSTS, CSP, X-Content-Type-Options, Referrer-Policy,
  Permissions-Policy) MUST be applied via middleware to all responses including 404s
  and error responses — see *Security Headers*.
- Missing or weakened security headers are a Snyk SAST blocking finding.

### V. SOLID (NON-NEGOTIABLE)
Every class and interface MUST comply with all five SOLID principles:

- **Single Responsibility**: Each class has exactly one reason to change.
- **Open/Closed**: Classes are open for extension, closed for modification. Extend via new types,
  not edits to existing ones.
- **Liskov Substitution**: Subtypes MUST be fully substitutable for their base types without
  altering correctness.
- **Interface Segregation**: Prefer many focused interfaces over one general-purpose interface.
  No client MUST depend on methods it does not use.
- **Dependency Inversion**: High-level modules MUST NOT depend on low-level modules. Both MUST
  depend on abstractions. Inject dependencies; never instantiate them inside a class.

SOLID violations MUST be raised in code review and MUST be remediated before merge.

### VI. Self-Documenting Code
Code MUST communicate intent through naming alone. Comments inside a class are a code smell.

- Method names MUST describe exactly what the method does. If a comment is needed to explain a
  method body, rename the method or extract a helper until no comment is needed.
- Variables, parameters, and types MUST be named at the level of intent, not implementation
  (e.g., `pendingCardIds` not `list`, `BoardRepository` not `Repo`).
- If a reader would be surprised by a behavior, that behavior belongs in the name, not a comment.
- The only permitted comments are: (a) public API XML doc comments on REST endpoint contracts and
  (b) links to external specifications or RFCs that cannot be expressed in code.

### VII. Established Design Patterns
All structural decisions MUST follow well-established GoF or DDD patterns. Ad-hoc structures
require documented justification in the plan's Complexity Tracking table.

Required patterns by layer:

| Layer | Required Pattern |
|-------|-----------------|
| Business layer | Fluent interface; Builders created via Factories |
| Domain | Aggregate roots, Value objects, Domain events |
| Data access | Repository pattern (Dapper against `IDbConnection`) |
| Anti-corruption | Adapter pattern wrapping all external API calls |
| REST API | Minimal API endpoint handlers; no business logic in endpoint handlers |
| React frontend | Container/Presentational split; custom hooks for shared logic |
| React components | Fluent UI 2 components; React Testing Library for component tests |

When selecting patterns, agents MUST query the Microsoft Learn MCP server for current best
practices for C# and React before proposing an implementation approach.

### VIII. MVP-Oriented Delivery
Every feature decision MUST be evaluated against the question: *does this move us toward a
product that users love?*

The SVPG definition of MVP governs: **MVP means minimum scope, not minimum quality.** An MVP
is the smallest set of features that delivers genuine value — something users actively choose
over alternatives, not merely tolerate. A low-quality, barely-functional release is not an MVP;
it is a prototype, and MUST NOT be shipped as a product.

- Every spec MUST identify which user problem is being solved and for whom.
- Features MUST be prioritized by user value, not engineering convenience.
- "Minimum" applies to scope; quality, usability, and performance standards are
  non-negotiable even in the first shipped increment.
- Agents and developers MUST challenge features that add scope without proportional user value.

### IX. Non-Functional Requirements (NFRs)
NFRs are first-class acceptance criteria on every feature. A feature that passes functional
tests but fails NFR gates is not complete.

#### Maintainability
- SOLID compliance and self-documenting code (see Principles V and VI) are the primary
  maintainability gates.
- Cyclomatic complexity per method MUST stay ≤ 10.
- Every public interface in `Kanban.Business` and `Kanban.Domain` MUST have an xUnit test
  exercising its contract so regressions are caught immediately.
- Agents MUST prefer extending existing abstractions over introducing new ones.

#### Usability
- All UI MUST be built with **Microsoft Fluent UI 2** (`@fluentui/react-components`).
  No custom component framework.
- All interactive components MUST meet **WCAG 2.1 AA** accessibility standards.
- Fluent UI 2 accessibility props (`aria-*`, keyboard navigation, focus management) MUST be
  used correctly on every component.
- User flows MUST be validated by Playwright e2e tests that exercise keyboard-only navigation.
- Component tests MUST use React Testing Library's accessibility queries
  (`getByRole`, `getByLabelText`) over implementation-detail selectors (`getByTestId`).

- **Fluent UI ARIA contract (non-negotiable):**
  - Typography components (`Title1`, `Title2`, `Body1`, etc.) render as `<span>` by default.
    When they serve as a page or section heading, they MUST use the `as="h1"` (or appropriate
    level) prop so that `getByRole('heading', { level: N })` locators work. Missing `as` props
    are both an a11y defect and a test-locator defect.
  - `MessageBar` renders as `role="group"` by default. Any `MessageBar` conveying a transient
    status or error MUST add `role="alert"` explicitly so it participates in the ARIA live
    region tree and is reachable via `getByRole('alert')`.
  - Every RTL component test for a page MUST assert `getByRole('heading', { level: 1 })` as
    an explicit WCAG AA gate. A page without a level-1 heading fails the accessibility audit.

#### Performance
- React component re-renders MUST be profiled; unnecessary re-renders MUST be eliminated
  before a feature is merged (use `React.memo`, `useMemo`, `useCallback` where measured).
- REST API response time MUST be ≤ 200ms at the 95th percentile for all read operations under
  normal load.
- Frontend initial bundle size MUST be monitored; new dependencies require bundle impact
  justification before merge.
- Database queries MUST use parameterized Dapper calls; N+1 queries are a blocking defect.

---

## Solution Structure

The solution MUST maintain this eight-project layout. No additional projects without a
documented rationale in the plan's Complexity Tracking table.

```
Kanban.sln
├── src/
│   ├── Kanban.Api/           # ASP.NET 10 REST API — minimal API endpoints only; no business logic
│   ├── Kanban.Web/           # React (Vite/TypeScript) + Fluent UI 2; calls REST API over HTTP
│   ├── Kanban.Business/      # Fluent interface, Builders, Factories; only layer that transforms
│   │                         #   Domain entities ↔ Contracts DTOs
│   ├── Kanban.Domain/        # Entities, Value objects, Aggregates, Domain events
│   ├── Kanban.Contracts/     # DTOs only — the only types that cross the API boundary
│   ├── Kanban.AntiCorruption/# Adapters for all external APIs (Google OAuth, etc.)
│   ├── Kanban.Data/          # Database schema, DbUp migration scripts (env-specific variants)
│   └── Kanban.DataAccess/    # Dapper repositories; depend on IDbConnection, never concrete types
└── tests/
    ├── unit/                 # xUnit + FluentAssertions
    ├── integration/          # xUnit + FluentAssertions (real SQLite in CI)
    └── e2e/                  # Playwright
```

### Layer Responsibilities (strict — no cross-layer leakage)

| Project | Depends on | MUST NOT depend on |
|---------|-----------|-------------------|
| `Kanban.Api` | Business, Contracts | Domain, DataAccess, Data |
| `Kanban.Web` | `Kanban.Api` (HTTP/REST) | Everything else |
| `Kanban.Business` | Domain, Contracts, DataAccess, AntiCorruption | Api, Data, Web |
| `Kanban.Domain` | nothing | Everything |
| `Kanban.Contracts` | nothing | Everything except shared primitives |
| `Kanban.AntiCorruption` | Domain, Contracts | Api, Business, DataAccess, Data |
| `Kanban.Data` | nothing | Everything |
| `Kanban.DataAccess` | Domain, Data | Api, Business, Contracts, Web |

---

## Domain-Driven Design Constraints

- **Entities MUST NEVER cross the API boundary.** The `Kanban.Api` layer MUST only send and
  receive `Kanban.Contracts` DTOs. All entity-to-DTO and DTO-to-entity transforms live
  exclusively in `Kanban.Business`.
- **Aggregate roots** are the only public entry points into a domain cluster. External code MUST
  NOT reach past an aggregate root to modify inner entities directly.
- **Value objects** MUST be immutable. Equality is structural, not referential.
- **Domain events** are raised by aggregates and handled by the Business layer. They MUST NOT
  cross the boundary into the API or DataAccess layers directly.
- **Ubiquitous language** used in the domain model MUST match the language used in specs and
  in conversations with stakeholders. Rename domain objects when language drifts.

---

## Domain Model Concepts

These are the governing concepts for `Kanban.Domain`. The SQL schema lives in the feature spec
and `Kanban.Data` migrations.

| Entity | Description |
|--------|-------------|
| **User** | Authenticated via Google OAuth; has identity, display name, avatar, and SystemRole |
| **Board** | Top-level container; owned by a User; has members with roles (owner/member/viewer) |
| **Lane** | Ordered column within a Board; has a name and an integer position |
| **Card** | Work item within a Lane; has title, description, due date, and an integer position |
| **CardAssignee** | Association between a Card and one or more Users |

**Ordering invariant**: Lane positions are unique per Board; Card positions are unique per Lane.
When a Card or Lane is moved, all affected sibling positions MUST be updated in a single
transaction. There is no gap-based or fractional indexing (Principle III — simplest correct
approach).

The full SQL schema (table definitions, constraints, indexes) belongs in the feature spec
and is authoritative once committed to `Kanban.Data/migrations/`.

---

## Authorization Model

### Three-Layer Access Model

1. **Google OAuth** — proves identity (valid token = authenticated)
2. **`RegisteredUser` policy** — proves the authenticated Google user has an active record in
   the `users` table (invited and accepted)
3. **`BoardMembershipRequirement`** — proves the registered user has the required role on the
   specific board being accessed

### Two-Level Role Model

- **System roles** (`User.SystemRole`): `Admin`, `Standard` — stored on the `User` entity
- **Board roles** (`board_members.Role`): `Owner`, `Member`, `Viewer` — stored per membership

### Operation Matrix

| Operation | Admin | Owner | Member | Viewer |
|-----------|-------|-------|--------|--------|
| Create board | ✓ | ✗ | ✗ | ✗ |
| Send invites | ✓ | ✓ | ✗ | ✗ |
| Read board / lanes / cards | ✓* | ✓ | ✓ | ✓ |
| Create / update / delete cards and lanes | ✓* | ✓ | ✓ | ✗ |
| Manage members | ✓* | ✓ | ✗ | ✗ |
| Delete board | ✓ | ✓ | ✗ | ✗ |

*Admin must be a board member to access board data. Creating a board automatically grants
Admin the Owner board role on that board. There is no ambient super-access to all boards.

### Authorization Policies

```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RegisteredUser", policy => policy
        .RequireAuthenticatedUser()
        .AddRequirements(new RegisteredUserRequirement()))
    .AddPolicy("Admin", policy => policy
        .RequireAuthenticatedUser()
        .RequireClaim("system_role", "admin"));
```

`RegisteredUserHandler` checks that the `sub` claim maps to an active `User` record in the DB.
Unregistered Google users receive `403` with Problem Details `code: "user.not_registered"` —
never `401` (they ARE authenticated, just not registered).

### System Role Claim Flow

The `system_role` claim is added during the OIDC `OnTicketReceived` callback after the `User`
record is resolved from the DB — stored in the auth session. No DB lookup on subsequent requests.

### Endpoint Authorization Structure

- `POST /api/v1/boards` — requires `Admin` policy
- `POST /api/v1/invites/{token}/accept` — requires `RequireAuthorization()` only
  (Google auth, not `RegisteredUser` — invitee is not yet registered)
- All other endpoints — require `RegisteredUser` policy via versioned endpoint group

### Invite Flow

1. Board owner or Admin sends invite (email + board + role) → creates `pending` `board_members`
   record
2. Invitee receives email with a single-use token expiring in 7 days
3. Invitee clicks link → Google OAuth → `POST /api/v1/invites/{token}/accept`
4. Accept endpoint: creates `User` record if absent, activates `board_members` record, marks
   token consumed

### Enumeration Prevention

Boards the requesting user is not a member of MUST return `404` — even if they exist.
`403` is only returned for operations on a board the user can see but lacks the role for.

### Resource-Based Authorization Implementation

- `BoardOperations` static class: `Read`, `CreateCard`, `UpdateCard`, `DeleteCard`,
  `CreateLane`, `UpdateLane`, `DeleteLane`, `ManageMembers`, `DeleteBoard`
- `BoardMembershipRequirement : IAuthorizationRequirement`
- `BoardAuthorizationHandler : AuthorizationHandler<BoardMembershipRequirement, BoardContext>`
- `BoardContext` — lightweight value type `{ BoardId, ResolvedRole }` loaded once per request
- Enforcement: Business layer service methods call `IAuthorizationService.AuthorizeAsync` —
  never in endpoint handlers
- Identity flow: `IHttpContextAccessor` → scoped `ICurrentUserService` resolves
  `User.ExternalId` from Google `sub` claim → injected into Business layer (same DI seam
  where `TenantContext` slots in for multi-tenancy)

### Initial Admin Seeding

The initial Admin `User` record is created in a DbUp seed migration. The email is configured
via the `Seed:AdminEmail` user secret — never hardcoded. On first Google login with that email
the OIDC callback matches the `email` claim to the seeded record and writes the `sub` claim
back, linking the Google identity permanently. Subsequent logins use `sub` only.

---

## Transaction Pattern

> Reference: https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions

SQLite allows only **one write transaction at a time**. Transactions are serializable by default.
All transaction handling MUST follow the patterns below, informed by the Microsoft Learn SQLite
transactions documentation.

### Standard pattern — deferred transaction

Use `BeginTransaction(deferred: true)` for any operation that reads before writing. A deferred
transaction starts as a read and upgrades to a write only when the first write executes,
maximising concurrent read access during the read phase.

```csharp
using var transaction = connection.BeginTransaction(deferred: true);
try
{
    // All Dapper calls must receive the transaction explicitly
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

**Retry with Polly**: a deferred transaction that attempts to upgrade from read to write while
the database is locked will throw. The Business layer MUST use **Polly** to wrap deferred write
operations with a retry policy (2–3 attempts, exponential backoff) before surfacing the error.
Polly is the standard resilience mechanism for all transient failures across the application —
database lock contention, external API timeouts, and any other retriable fault.

### Optimistic concurrency — savepoints

For concurrent update scenarios (e.g. two users moving the same card), use savepoints:

```csharp
using var transaction = connection.BeginTransaction();
var updated = false;
do
{
    transaction.Save("optimistic-update");
    var rowsAffected = /* Dapper UPDATE with version/timestamp check */;
    if (rowsAffected == 0)
    {
        transaction.Rollback("optimistic-update");
        // resolve conflict, then retry within the same outer transaction
    }
    else
    {
        transaction.Release("optimistic-update");
        updated = true;
    }
} while (!updated);
transaction.Commit();
```

### Rules
- **No `TransactionScope`** — use `IDbTransaction` directly. `TransactionScope` promotes to DTC,
  which SQLite does not support.
- Every Dapper call inside a transaction MUST receive the `IDbTransaction` instance explicitly.
- **No external API calls, I/O, or network operations inside a transaction.** Acquire all
  external data before opening the transaction. See *HTTP Resilience* for the outbound
  HTTP pattern — outbound calls MUST happen before `BeginTransaction()`.
- Transactions MUST be kept as short as possible — open late, commit early.
- This pattern is portable to PostgreSQL: deferred transactions and savepoints are supported
  in Npgsql with equivalent semantics.

---

## Database Portability

SQLite is used for local development (zero config, file-based). The architecture MUST make
swapping to PostgreSQL for production a configuration change only.

- `Kanban.DataAccess` MUST depend only on `IDbConnection`. `SqliteConnection` and
  `NpgsqlConnection` MUST never appear outside the DI registration in `Kanban.Api`.
- The connection factory is the single swap point: change the NuGet package, connection string,
  and DI registration. No other code changes are permitted for a DB swap.
- Migration scripts in `Kanban.Data` are organized into `migrations/sqlite/` and
  `migrations/postgres/` subfolders. Scripts MUST use standard ANSI SQL where possible;
  engine-specific syntax is isolated per folder.
- Avoid `RETURNING` in INSERT statements — use a separate SELECT or query-after-insert pattern
  that works in both engines.
- All CI integration tests run against SQLite. Postgres-specific behavior is validated in a
  staging environment.
- When deployed in containers, the recommended database is Postgres — see
  *Containerization Readiness — SQLite in Containers* for the rationale.

---

## Multi-Tenancy Readiness

Multi-tenancy is **out of scope for MVP**. However, the architecture MUST make it achievable
via targeted migration — not a rewrite. The following seam decisions are made now, mirroring
the approach taken for database portability.

- **All data access is user-scoped from day one.** No repository method returns data without a
  `userId` or equivalent principal in scope. There are no "list everything" queries that would
  accidentally cross tenant boundaries. Adding `tenantId` alongside `userId` later is additive.
- **Authentication context flows through DI, not method parameters.** The authenticated user's
  identity is resolved from a scoped service (claims from `IHttpContextAccessor`). This is the
  same seam where a `TenantContext` would later be inserted — one registration change, not a
  parameter-chain refactor across every method signature.
- **Board is already the natural tenant boundary.** The Board membership model (owner + members
  with roles) resembles an org-scoped access model. When multi-tenancy is introduced, an
  `Organization` aggregate wrapping Boards is the expected migration path.
- **GUIDs for all primary keys.** GUID-keyed entities are tenant-portable and safe for
  cross-shard or cross-database routing without collisions.
- **No static or ambient application state.** All services are DI-registered as scoped or
  transient. A scoped `TenantContext` can be added and injected into any repository or business
  service without architectural surgery.
- **`IDbConnection` abstraction** also enables database-per-tenant routing: the connection
  factory can resolve the correct connection string by tenant ID without any repository changes.

When multi-tenancy is introduced, the migration path is:
1. Add `Organization` aggregate and `tenant_id` to `boards` (cascade as needed).
2. Register a scoped `TenantContext` resolved from the auth token.
3. Inject `TenantContext` into repositories alongside the existing user context.
4. Update the connection factory if database-per-tenant isolation is required.

---

## Containerization Readiness

Production deployment is expected to be containerized. The architecture MUST make this a
deployment-time decision, not a code change. The following seams are decided now to mirror
the approach taken for *Database Portability* and *Multi-Tenancy Readiness*.

### Core Principles

- **Stateless API.** No static or ambient state. No in-memory session, no in-memory cache
  for cross-request data. All services DI-registered as scoped or transient (already enforced
  by Principle V and the multi-tenancy seam — reinforced here for containers). Any future
  caching MUST go through `IDistributedCache` so the implementation can swap from in-memory
  (single replica) to Redis (multi-replica) at registration time without code changes.
- **Configuration via environment variables.** No file-based secrets in containers. User
  secrets are a local-dev-only mechanism; production reads configuration exclusively from
  environment variables or a secrets manager via `IConfiguration` providers
  (see *Local Secrets Management* and *Configuration & Options Pattern*).
- **Logs to stdout only.** No file-based log providers (`AddFile()` is forbidden). The
  orchestrator collects stdout/stderr. `AddJsonConsole()` is the production output (already
  mandated in *Structured Logging*).
- **Idempotent, fast startup.** Migrations run on application startup but MUST be safe under
  concurrent replica boot — see *Health & Lifecycle — Migration Coordination*.

### Image Strategy

| Concern | Decision |
|---------|----------|
| Runtime base | `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` — Microsoft-recommended distroless-style image for .NET 10; ~30MB, non-root by default |
| Build base | `mcr.microsoft.com/dotnet/sdk:10.0` — multi-stage build, final image contains only runtime + published output |
| User | Non-root (chiseled image's built-in `app` user) |
| Exposed port | 8080 (chiseled default — no root required) |
| Frontend image | Separate static container in production (CDN-friendly, scale-independent). Local dev continues to use Vite dev server. |

### `.dockerignore`

MUST be committed to the repo root alongside the eventual Dockerfile. MUST exclude at minimum:

```
**/bin/
**/obj/
**/node_modules/
.git/
.vs/
.vscode/
tests/
*.user
*.suo
**/.env*
**/appsettings.Development.json
```

The user-secrets store (`~/.microsoft/usersecrets/`) is outside the build context, but
`appsettings.Development.json` is excluded defensively.

### Image Scanning

- **Snyk Container** MUST run in CI on every published image. Medium/high/critical findings
  block merge — same gate as SCA and SAST (Principle IV).
- Images MUST be published with an immutable tag (`sha-<commit>`) plus a mutable `latest`
  tag. Production deploys MUST reference the immutable tag.

### SQLite in Containers — Caveat

SQLite is the local-dev DB. In a containerized deploy SQLite requires a mounted volume and
is incompatible with multi-replica deploys (single-writer constraint, no shared filesystem).
**The recommended container deploy path is Postgres** — already supported by the
`IDbConnection` seam (see *Database Portability*). Single-replica SQLite deploys with a
mounted volume are technically supported but should be reserved for demo / single-tenant
pilot scenarios.

### Forwarded Headers — Mandatory When Behind Ingress

When the API runs behind ingress / load balancer (the default container deploy topology),
`UseForwardedHeaders` MUST be the FIRST middleware — before `UseRouting`. Without it OIDC
redirects break (scheme appears as `http` not `https`) and client IP logging is wrong.

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions {
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseRouting();
app.UseCors("KanbanWebApp");
app.UseAuthentication();
app.UseAuthorization();
```

`KnownNetworks` / `KnownProxies` MUST be configured for the production ingress range —
accepting forwarded headers from arbitrary upstreams is a spoofing vector.

---

## Health & Lifecycle

Container orchestrators (Kubernetes, Azure Container Apps, ECS) require liveness and
readiness probes to perform rolling deploys without dropping requests. These endpoints are
mandatory regardless of whether the MVP is containerized — they are zero-cost to add now
and standard infrastructure when needed.

### Health Check Endpoints

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

app.MapHealthChecks("/health/live", new HealthCheckOptions {
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions {
    Predicate = check => check.Tags.Contains("ready")
});
```

| Endpoint | What it checks | Auth |
|---------|----------------|------|
| `/health/live` | Process is responsive — does NOT touch DB or any dependency | None |
| `/health/ready` | All dependencies (DB connectivity) are healthy | None |

**Rules (non-negotiable):**

- Liveness MUST NOT touch the database. A slow query MUST NOT cause the orchestrator to
  kill the container.
- Readiness MUST verify DB connectivity via a lightweight query (`SELECT 1`).
- Neither endpoint requires authentication — orchestrator probes do not carry tokens.
- Both endpoints MUST be excluded from request logging — they fire every few seconds and
  would drown the log stream.

### Graceful Shutdown

```csharp
builder.Services.Configure<HostOptions>(opts => {
    opts.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

- On SIGTERM, ASP.NET stops accepting new requests and waits up to `ShutdownTimeout` for
  in-flight requests to complete.
- Long-running background work MUST observe `IHostApplicationLifetime.ApplicationStopping`
  and cancel cooperatively.
- Database connections are disposed via DI scope disposal — no manual cleanup required.

### Migration Coordination on Multi-Replica Startup

When multiple replicas start simultaneously they all attempt DbUp migrations. The DbUp
`SchemaVersions` journal table provides the lock naturally — DbUp begins a transaction,
reads the journal, and only one replica's transaction will succeed in adding a new entry.
Losing replicas observe the migration already applied and proceed.

**No explicit init container or one-shot job is required for MVP.** If a future migration
is destructive or long-running, that specific migration MUST be promoted to a one-shot job
pattern — but the default path stays in-process.

---

## Error Handling

### Backend — Three-Layer Setup in `Program.cs`

```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<InfrastructureExceptionHandler>();
builder.Services.AddExceptionHandler<FallbackExceptionHandler>();

app.UseExceptionHandler();      // activates the IExceptionHandler chain
app.UseStatusCodePages();       // ensures 4xx/5xx without a body get RFC 7807 bodies
```

- `DeveloperExceptionPage` is enabled by default in Development — full stack trace visible.
  It MUST NEVER be active in production.
- Every error response MUST include a `traceId` field (`Activity.Current?.Id`) for correlation.
- Production responses MUST NEVER contain: stack traces, exception class names, SQL text,
  file paths, or internal IDs.

### Custom Exception Hierarchy

Exceptions live in `Kanban.Domain` (domain rules) and `Kanban.Business` (application-level).
They have no knowledge of HTTP — status code mapping happens exclusively in middleware.

```
KanbanException (abstract base)
├── DomainException (abstract)
│   ├── NotFoundException           → 404
│   ├── ForbiddenException          → 404 (board enumeration) or 403 (known-board operation)
│   ├── ConflictException           → 409 (optimistic concurrency, duplicate names)
│   └── BusinessRuleException       → 422 (business rule violations)
└── InfrastructureException (abstract)
    ├── DataAccessException         → 500 (DB failure after Polly exhausted)
    └── ExternalServiceException    → 502 (Google OAuth, ACL failures)
```

Each exception carries a `Code` string (e.g. `"board.not_found"`) surfaced in the Problem
Details `code` extension field for typed frontend error handling.

### IExceptionHandler Chain

- **`DomainExceptionHandler`** — maps `DomainException` hierarchy to status codes; writes
  Problem Details with `code` and `traceId`
- **`InfrastructureExceptionHandler`** — maps `InfrastructureException` hierarchy
- **`FallbackExceptionHandler`** — logs the full exception at `Error` level internally;
  returns ONLY `{ title: "An unexpected error occurred", traceId: "..." }` to the client —
  no `detail`, no stack trace, no internal names

### Parameter Verification — Fluent `Verify` Class

Lives in `Kanban.Domain` (no dependencies; accessible to all layers).

**Rule:** ALL public method parameters that are non-nullable and non-optional MUST be verified
as the very first statements in the method body. Nullable (`string?`, `Guid?`) and optional
(default value) parameters are exempt.

```csharp
// Static entry point — CallerArgumentExpression captures param name automatically
public static class Verify
{
    public static ParameterVerifier<T> That<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
        => new(value, paramName);
}

// Usage — no nameof() required
public Card CreateCard(string title, Guid laneId, int position, string? description)
{
    Verify.That(title).IsNotNull().IsNotEmpty().HasMaxLength(200);
    Verify.That(laneId).IsNotDefault();
    Verify.That(position).IsNonNegative();
    // description is nullable — exempt
}
```

**`ParameterVerifier<T>` core methods:** `IsNotNull()` → `ArgumentNullException`,
`IsNotDefault()` → `ArgumentException` (catches `Guid.Empty`, `0`, `false`, etc.)

**Type-specific extension methods (generic constraints):**

| Extension | Constraint | Checks |
|-----------|-----------|--------|
| `IsNotEmpty()` | `ParameterVerifier<string>` | non-empty string |
| `HasMaxLength(int)` | `ParameterVerifier<string>` | length ≤ max |
| `IsNotEmpty<T>()` | `IEnumerable<T>` | non-empty collection |
| `IsPositive<T>()` | `INumber<T>` (.NET 10) | value > 0 |
| `IsNonNegative<T>()` | `INumber<T>` (.NET 10) | value ≥ 0 |
| `IsGreaterThan<T>(T)` | `IComparable<T>` | value > threshold |
| `IsInRange<T>(T, T)` | `IComparable<T>` | min ≤ value ≤ max |

`Verify` throws `ArgumentNullException` / `ArgumentException` — programming errors that map
to `500` via `FallbackExceptionHandler`. If `Verify` fires in production, a layer boundary
failed to validate before calling. `FluentValidation` on DTOs catches user-input problems
before they ever reach a `Verify` call.

**Scope:**

| Layer | Required? |
|-------|-----------|
| `Kanban.Business` services | Yes |
| `Kanban.DataAccess` repositories | Yes |
| `Kanban.Domain` entity / value object constructors and methods | Yes |
| `Kanban.AntiCorruption` adapters | Yes |
| `Kanban.Api` endpoint handlers | No — `FluentValidation` handles the API boundary |

### Frontend Error Handling

**Retry policy:** TanStack Query retries `5xx` / network errors up to 2 times with exponential
backoff. `4xx` responses are NEVER retried — they are deterministic failures.

**Reconnection:** TanStack Query's `onlineManager` handles browser offline/online transitions
automatically — stale queries re-fetch on reconnect without custom code.

**Failed mutations:** MUST show a persistent (non-auto-dismissing) Fluent UI toast with a
"Try again" action. The user is never silently left with stale state.

**Error boundaries** — three levels using `react-error-boundary`:

| Boundary | Scope | Fallback |
|----------|-------|---------|
| `BoardPageErrorBoundary` | Entire board page | Full-page error with retry |
| `LaneErrorBoundary` | Single lane | Inline lane error; other lanes unaffected |
| `CardErrorBoundary` | Single card | Card placeholder; lane unaffected |

Error boundaries are NEVER placed at the app root — a single broken card MUST NOT take down
the board.

### Data Loss Prevention (Non-Negotiable)

1. **Distinguish empty from failed**: `null` / `undefined` where data is expected triggers the
   error boundary — never renders as an empty state. An empty board (0 lanes returned) is valid;
   a `null` lanes array is an error.
2. **Preserve drafts on mutation failure**: when a save mutation fails the form MUST restore the
   user's in-progress edit — the draft is never discarded on API error.
3. **Optimistic rollback is visible**: when a card move fails the card visually returns to its
   origin position AND a toast explains why — the user is never left with a board that silently
   disagrees with the server.
4. **Conflict must not silently succeed**: `ConflictException` (409) MUST surface as
   "This item was modified by someone else — please refresh" — never silently overwrite.
5. **Partial response = error**: if an API response omits required fields the frontend treats
   this as a failed response and triggers the appropriate error boundary — never renders a false
   empty state that could mislead the user into thinking data is gone.

---

## Frontend State Management

**TanStack Query v5** is the mandatory server state management solution.
No Redux, Zustand, or Jotai for MVP.

### Setup

- Packages: `@tanstack/react-query`, `@tanstack/react-query-devtools` (dev only)
- `QueryClient` at app root: `staleTime: 30_000`; retry policy retries `5xx` / network errors
  up to 2 times — NEVER retries `4xx`
- `QueryClientProvider` wraps `<App />`
- `ReactQueryDevtools` rendered in development builds only

### Query Key Convention

Hierarchical arrays enable precise cache invalidation:

```
['boards']                                    — board list
['boards', boardId]                           — single board
['boards', boardId, 'lanes']                  — lanes for a board
['boards', boardId, 'lanes', laneId, 'cards'] — cards in a lane
```

### Custom Hook Layer

Components MUST NOT use `useQuery` / `useMutation` directly. All data access goes through
named custom hooks in `src/hooks/`:

- **Read hooks**: `useBoards()`, `useBoard(id)`, `useLanes(boardId)`, `useCards(laneId)`
- **Mutation hooks**: one hook per mutation — `useCreateBoard()`, `useUpdateCard()`,
  `useMoveCard()`, `useDeleteLane()`, etc.

This is where the Container/Presentational split is achieved — components import hooks, not
raw TanStack Query primitives.

### Mutation Pattern

```
onMutate  → optimistic cache update; snapshot previous cache state
onError   → rollback to snapshot + persistent toast with "Try again"
onSettled → invalidateQueries to sync authoritative server state
```

### State Split Rule

- **Server state** (anything persisted in the DB) → TanStack Query exclusively
- **UI state** (modal open/close, drag in progress, dirty form drafts) → `useState` /
  `useReducer` or a small `useContext`

### Testing

RTL tests use a `createQueryClientWrapper()` test utility providing a fresh `QueryClient`.
Mocks target the fetch layer (msw) — not TanStack Query internals.

### StrictMode and One-Shot Mutations

React 18 StrictMode intentionally double-invokes effects (mount → simulated unmount → remount)
in development to surface side-effect bugs. Two rules govern any component that auto-fires a
mutation on mount:

**Rule 1 — Guard with `useRef`:**

Any `useEffect` that triggers a one-shot operation (a mutation that should fire exactly once)
MUST use a `useRef(false)` guard. `useRef` survives StrictMode's simulated unmount; mutation
state and TanStack Query `MutationObserver`s do not.

```tsx
const hasAttempted = useRef(false)
useEffect(() => {
  if (hasAttempted.current) return
  if (conditionMet) {
    hasAttempted.current = true
    // fire the one-shot operation here
  }
}, [conditionMet])
```

**Rule 2 — Use `mutateAsync` when chaining imperative logic:**

When a mutation's result drives navigation, a cascade, or any imperative follow-up action,
use `mutateAsync` not `mutate`:

```tsx
// ✅ Promise chain survives StrictMode remount
mutateAsync(payload).then(() => navigate('/')).catch(() => {})

// ❌ useEffect watching data never fires — MutationObserver is destroyed on remount
mutate(payload)
useEffect(() => { if (data) navigate('/') }, [data, navigate])
```

`mutateAsync` returns a Promise whose `.then()` callbacks fire when the Promise resolves,
regardless of component remount. `navigate` from `useNavigate()` is a stable reference in
React Router v6 and is safe to call outside the render cycle.

Use `mutate` (not `mutateAsync`) only for fire-and-forget mutations where the UI reads
`data` / `error` declaratively on the next render cycle.

---

## Drag and Drop

**Packages:** `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities`

### Sensors

`PointerSensor` + `KeyboardSensor` only. `MouseSensor` is explicitly excluded — it is less
accessible and redundant with `PointerSensor`. Keyboard drag-and-drop is required for
WCAG 2.1 AA compliance.

### Component Structure

```
<DndContext sensors onDragEnd onDragStart onDragOver>
  <SortableContext items={laneIds} strategy={horizontalListSortingStrategy}>
    {lanes.map(lane =>
      <LaneErrorBoundary>
        <SortableContext items={cardIds} strategy={verticalListSortingStrategy}>
          {cards.map(card => <SortableCard />)}
        </SortableContext>
      </LaneErrorBoundary>
    )}
  </SortableContext>
  <DragOverlay>
    {activeItem && <CardDragPreview card={activeItem} />}
  </DragOverlay>
</DndContext>
```

`DragOverlay` renders at root level — avoids stacking context conflicts with Fluent UI portals.

### Move Cases in `onDragEnd`

- **Same lane**: reorder within the lane's card array by index
- **Different lane**: splice out of source lane, insert at target position in destination lane

### Optimistic Update Flow (via TanStack Query)

```
onDragEnd   → fire useMoveCard({ cardId, targetLaneId, newPosition })
onMutate    → snapshot current cache; apply optimistic reorder immediately
onError     → restore snapshot; card visually returns to origin + persistent toast
onSettled   → invalidateQueries(['boards', boardId]) — server positions become authoritative
```

### Accessibility (WCAG AA Gate)

- `announcements` prop on `DndContext`: "Picked up card X", "Moved to lane Y position Z",
  "Drop cancelled"
- `aria-describedby` on each draggable card pointing to a visually hidden instructions element
- Focus returns to the moved card after a successful drop
- Keyboard: Space to pick up, arrow keys to move, Space to drop, Escape to cancel

---

## CORS Configuration

Named policy `"KanbanWebApp"` applied globally. No per-endpoint decoration needed.

### Policy

```csharp
builder.Services.AddCors(options => options.AddPolicy("KanbanWebApp", policy =>
    policy
        .WithOrigins(builder.Configuration
            .GetSection("Cors:AllowedOrigins").Get<string[]>()!)
        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
        .AllowAnyHeader()
        .WithExposedHeaders("X-Correlation-Id", "api-supported-versions")));
```

### Origins — Environment-Driven, Never Hardcoded

```json
// appsettings.Development.json
"Cors": { "AllowedOrigins": ["http://localhost:5173"] }
// Production: environment variable Cors__AllowedOrigins__0
```

`AllowCredentials()` is NOT used — JWTs are sent in the `Authorization` header, not cookies.
Adding it without a specific need introduces CSRF risk with no benefit.

### Mandatory Middleware Order

```csharp
app.UseRouting();
app.UseCors("KanbanWebApp");      // MUST be before authentication
app.UseAuthentication();
app.UseAuthorization();
```

Wrong middleware order silently breaks authentication. This order is non-negotiable.

When the API runs behind ingress (the default containerized topology), `UseForwardedHeaders`
MUST be added BEFORE `UseRouting` — see *Containerization Readiness — Forwarded Headers*.
The CORS position relative to authentication is unchanged.

### Hard Prohibitions (Snyk SAST Enforced)

- `AllowAnyOrigin()` is **forbidden**
- `AllowAnyOrigin()` + `AllowCredentials()` is a **critical violation**

### Google OAuth Redirect

The OIDC middleware handles the `/signin-google` callback server-side — CORS does not apply
to the auth redirect URI. No special CORS carve-out is needed for authentication.

---

## API Versioning

**Package:** `Asp.Versioning.Http` — Microsoft-maintained versioning library for minimal APIs.

**Strategy: URL path segment** — `/api/v1/boards`. Clearest for humans, most cacheable, best
tooling support, and directly importable into Azure API Management as distinct API versions.

### Setup

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true; // adds api-supported-versions response header
});
```

### Endpoint Registration

```csharp
var v1 = app.NewVersionedApi();
var v1Group = v1.MapGroup("/api/v1")
               .HasApiVersion(1, 0)
               .RequireAuthorization("RegisteredUser");
```

### Rules

- All endpoints MUST be declared as v1.0 explicitly. Adding v2 is a new route group —
  additive, not a refactor. No "we'll version it later" debt.
- When v2 ships, mark v1 as deprecated — the `api-deprecated-versions` response header signals
  clients to migrate. v1 MUST remain live for a defined sunset window.
- Each version produces its own OpenAPI document (`/openapi/v1.json`, `/openapi/v2.json`).
  Azure API Management imports these as separate API revisions.

---

## OpenAPI

**Packages:** `Microsoft.AspNetCore.OpenApi` (built-in with .NET 10) +
`Scalar.AspNetCore` (Microsoft-recommended modern replacement for Swagger UI in .NET 9+).

### Setup — Development Only

```csharp
if (builder.Environment.IsDevelopment())
    builder.Services.AddOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // available at /scalar/v1
}
```

OpenAPI endpoints MUST NEVER be exposed in production. The `IsDevelopment()` guard is
non-negotiable. Snyk SAST flags an exposed `/openapi` endpoint in production config.

### Per-Endpoint Documentation

Required on every endpoint:

```csharp
app.MapGet("/api/v1/boards/{id}", ...)
    .WithName("GetBoard")
    .WithSummary("Get a board by ID")
    .Produces<BoardDto>(200)
    .ProducesProblem(404)
    .ProducesProblem(403)
    .RequireAuthorization("RegisteredUser");
```

`WithName()` operationIds MUST be consistent — they become Azure API Management operation
identifiers and `openapi-typescript` function names.

### DTO Schema Documentation

XML doc comments on all `Kanban.Contracts` types auto-populate field descriptions in the
generated schema — one source of truth, no duplication between code and docs.

### Forward Path

- **`openapi-typescript`**: generates typed fetch clients for `Kanban.Web` from the OpenAPI
  document — eliminates manual DTO duplication between C# and TypeScript
- **Azure API Management**: imports the OpenAPI document to auto-create the API definition,
  configure rate limiting, subscription keys, caching policies, and a developer portal

Neither is required for MVP but both are unlocked at zero cost by maintaining a clean,
consistently named OpenAPI document.

---

## Logging & Observability

Logs, traces, and metrics MUST be treated as one observability surface — emitted via
OpenTelemetry and exported over OTLP. `ILogger<T>` remains the in-code API for logs;
OpenTelemetry is the exporter layer that ships logs, traces, and metrics out of the
process together.

**Library:** Built-in `Microsoft.Extensions.Logging` / `ILogger<T>` for the in-code logging
API. No Serilog for MVP (YAGNI — built-in structured logging covers all requirements).
OpenTelemetry SDK + OTLP exporter for shipping signals out.

### Injection Scope

`ILogger<T>` injected via constructor in `Kanban.Business` and `Kanban.Api` only.
`Kanban.Domain` has no logging — domain logic is pure.

### Structured Templates

String interpolation is **forbidden** in log messages:

```csharp
// Correct — structured, indexed, no allocations
_logger.LogInformation("Board {BoardId} retrieved for user {UserId}", boardId, userId);

// Forbidden — defeats structured logging and allocates unnecessarily
_logger.LogInformation($"Board {boardId} retrieved for user {userId}");
```

### Log Level Policy

| Level | What goes here |
|-------|---------------|
| Debug | Query parameters, SQL text (dev only — disabled in production) |
| Information | Successful CRUD, auth events (login, logout, invite accepted) |
| Warning | 401/403 events, validation failures, Polly retries, slow queries >100ms |
| Error | Unhandled exceptions, DB failures after Polly exhausted, OAuth errors |
| Critical | Application startup failure |

### PII and Sensitive Data (Non-Negotiable)

- **NEVER log**: email addresses, OAuth tokens, connection strings, card content, user display
  names
- **Safe to log**: GUIDs (`boardId`, `userId`, `cardId`), HTTP status codes, durations,
  system role

### Correlation ID Middleware

Every inbound request MUST be assigned a correlation ID for end-to-end tracing across
services and frontend logs.

```csharp
app.Use(async (ctx, next) => {
    var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                      ?? Activity.Current?.Id
                      ?? Guid.NewGuid().ToString("N");
    Activity.Current?.SetTag("correlation.id", correlationId);
    ctx.Response.Headers["X-Correlation-Id"] = correlationId;
    using (_logger.BeginScope(new { CorrelationId = correlationId })) {
        await next();
    }
});
```

- Inbound `X-Correlation-Id` header is honored if present (frontend may set one for
  user-reported bug tracking).
- Absent → derived from `Activity.Current.Id`; falls back to a fresh GUID only if no
  Activity is active (shouldn't happen with ASP.NET auto-instrumentation).
- Echoed in the response header — already exposed via CORS `WithExposedHeaders`.

### Distributed Tracing

ASP.NET, `HttpClient`, and Dapper are auto-instrumented via OpenTelemetry packages —
no manual code required for those layers. **Business-layer operations MUST be wrapped
in custom spans** so a trace shows the work, not just the HTTP and SQL.

```csharp
// In Kanban.Business — one ActivitySource per project
private static readonly ActivitySource Source = new("Kanban.Business");

public async Task<Card> MoveCardAsync(MoveCardCommand cmd) {
    using var activity = Source.StartActivity("MoveCard");
    activity?.SetTag("card.id", cmd.CardId);
    activity?.SetTag("target.lane.id", cmd.TargetLaneId);
    // ...
}
```

- One `ActivitySource` per project (`Kanban.Business`, `Kanban.Api`,
  `Kanban.AntiCorruption`). Domain has no tracing — domain logic is pure.
- Tag values MUST follow the PII rules above — GUIDs and status codes only, never
  emails or card content.
- `activity?.SetStatus(ActivityStatusCode.Error, "...")` MUST be called when an
  exception is caught and rethrown — otherwise the span shows green in the trace UI.

### Metrics

These signals MUST be emitted from MVP:

| Metric | Source | Type |
|--------|--------|------|
| `http.server.request.duration` | ASP.NET auto-instrumentation | histogram |
| `kanban.db.query.duration` | Dapper interceptor / manual `Meter` | histogram |
| `kanban.auth.failure` | Auth middleware | counter |
| `kanban.ratelimit.rejected` | Rate limiter (see *Rate Limiting*) | counter |

`Meter` instances follow the same naming as `ActivitySource` — one per project.

### OpenTelemetry Exporter

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Kanban.Api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("Kanban.Business", "Kanban.Api", "Kanban.AntiCorruption")
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("Kanban.Business", "Kanban.Api")
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(o => o
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Kanban.Api"))
    .AddOtlpExporter());
```

- Exporter endpoint is read from `OTEL_EXPORTER_OTLP_ENDPOINT` (OTel SDK convention).
- If the env var is unset, the exporter emits to the default `http://localhost:4317`.
  In production this MUST be set explicitly to the collector address. In container
  deploys it points at the sidecar / DaemonSet collector.
- **No application code changes between local dev, staging, and production** — only
  the endpoint env var differs.

### Local Dev Observability Backend — Aspire Dashboard

The .NET Aspire Dashboard runs as a single container and accepts OTLP from any source.
Local dev SHOULD have it running — observability is then real from day one.

```bash
docker run --rm -d \
  -p 18888:18888 \
  -p 4317:18889 \
  --name aspire-dashboard \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

- Dashboard UI: http://localhost:18888
- OTLP gRPC endpoint: http://localhost:4317 (default — no env var needed)
- Zero login required for local dev; one-token login for shared dev environments.

### Container & Production Output

- **Container stdout** remains required (already mandated in *Containerization
  Readiness*). `AddJsonConsole()` continues to ship structured JSON logs to stdout
  regardless of whether OTel is active — logs are dual-emitted (stdout + OTLP) so log
  aggregators that read stdout still work.
- Production OTel endpoint is supplied via `OTEL_EXPORTER_OTLP_ENDPOINT` environment
  variable — Azure Application Insights, Honeycomb, Grafana Cloud, or any
  OTLP-compatible backend.

### Configuration

```json
// appsettings.json — production minimum
"Logging": { "LogLevel": { "Default": "Warning", "Kanban": "Warning" } }

// appsettings.Development.json
"Logging": { "LogLevel": { "Default": "Warning", "Kanban": "Debug" } }
```

- **Production output**: `AddJsonConsole()` — single-line structured JSON parseable by log
  aggregators
- **Development output**: default console provider — human-readable

---

## HTTP Resilience

All outbound HTTP MUST go through `IHttpClientFactory` with declarative resilience —
never `new HttpClient()`, never raw `HttpClient` singletons. This applies to the Google
OIDC adapter today and any future external API.

### Package

`Microsoft.Extensions.Http.Resilience` — the modern Polly-on-rails package from
Microsoft. Adds timeout, retry, and circuit breaker as a single standard handler.

### Pattern — Named Clients in Kanban.AntiCorruption

```csharp
builder.Services.AddHttpClient<GoogleOidcAdapter>(client => {
    client.BaseAddress = new Uri("https://oauth2.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
}).AddStandardResilienceHandler();
```

`AddStandardResilienceHandler()` provides Microsoft's default policies:
- Total request timeout: 30s
- Per-attempt timeout: 10s
- Retries: 3 with exponential backoff + jitter on 5xx / network errors (never on 4xx)
- Circuit breaker: opens at 10% failure ratio over 30s, half-open after 5s

### Rules

- ALL external HTTP calls MUST be wrapped by an Adapter in `Kanban.AntiCorruption`
  (Principle VII pattern requirement) AND registered through `AddHttpClient<T>`.
- No `HttpClient` direct instantiation anywhere. `new HttpClient()` is a Snyk SAST flag.
- **No transaction MAY contain an outbound HTTP call** — already mandated in
  *Transaction Pattern*, restated here. External data MUST be fetched before
  `BeginTransaction()`.
- Resilience defaults MUST NOT be loosened without a documented justification in the
  feature plan's Complexity Tracking table.
- Custom policies (different retry counts, no circuit breaker, etc.) are permitted only
  when the external API contract demands it — e.g., webhook delivery may need longer
  retry windows.

---

## Rate Limiting

Built-in ASP.NET 10 rate limiting (`AddRateLimiter`). No third-party package required.

### Mandatory Policies

| Policy | Limit | Applied To |
|--------|-------|-----------|
| `anonymous` | 10 requests / minute per IP (fixed window) | `/api/v1/invites/{token}/accept`, OIDC callback `/signin-google` |
| `authenticated` | 100 requests / minute per user (sliding window) | All endpoints under `RegisteredUser` policy |
| `mutating` | 30 requests / minute per user (sliding window) | All POST / PUT / PATCH / DELETE on `RegisteredUser` endpoints |

### Setup

```csharp
builder.Services.AddRateLimiter(opts => {
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opts.OnRejected = async (ctx, ct) => {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        await Results.Problem(
            title: "Too many requests",
            statusCode: 429,
            detail: "Please retry after the Retry-After interval."
        ).ExecuteAsync(ctx.HttpContext);
    };

    opts.AddFixedWindowLimiter("anonymous", o => {
        o.PermitLimit = 10;
        o.Window = TimeSpan.FromMinutes(1);
    });
    opts.AddSlidingWindowLimiter("authenticated", o => {
        o.PermitLimit = 100;
        o.Window = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
    });
});

app.UseRateLimiter(); // AFTER UseAuthentication so we partition by user
```

### Rules

- Limits MUST be configured via `RateLimitOptions` (typed options — see *Configuration
  & Options Pattern*). Hardcoded constants in `Program.cs` are not acceptable.
- 429 responses MUST follow RFC 7807 Problem Details and include a `Retry-After` header.
- Rate-limit rejections MUST emit the `kanban.ratelimit.rejected` counter metric (see
  *Logging & Observability — Metrics*).
- The `anonymous` policy uses IP as the partition key — behind ingress, the client IP
  comes from `X-Forwarded-For` (handled by `UseForwardedHeaders` — see *Containerization
  Readiness*).
- The `authenticated` and `mutating` policies use the authenticated user's `sub` claim
  as the partition key.

### Distributed Rate Limiting — Future

Built-in rate limiting is **in-memory per replica**. With multiple replicas behind a
load balancer, each replica enforces its own limit independently — effective limit is
N × configured limit. For MVP this is acceptable (single-replica deploy is the default).
When multi-replica deploys ship, rate limiting MUST migrate to a Redis-backed limiter
(same `IDistributedCache` seam as *Containerization Readiness — Stateless API*). No code
change in handlers — only the limiter registration changes.

---

## Security Headers

Browsers enforce defense-in-depth via response headers. Missing headers are a Snyk SAST
flag and an OWASP Top 10 item (A05: Security Misconfiguration).

### Package

`NetEscapades.AspNetCore.SecurityHeaders` — the de facto standard package, maintained by
Andrew Lock, used by Microsoft samples.

### Required Headers

```csharp
app.UseSecurityHeaders(policies => policies
    .AddDefaultSecurityHeaders()
    .AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 60 * 60 * 24 * 365)
    .AddContentSecurityPolicy(builder => {
        builder.AddDefaultSrc().Self();
        builder.AddScriptSrc().Self();                     // NO 'unsafe-inline'
        builder.AddStyleSrc().Self().UnsafeInline();       // Fluent UI carve-out
        builder.AddImgSrc().Self().Data().From("https://lh3.googleusercontent.com");
        builder.AddConnectSrc().Self();
        builder.AddFrameAncestors().None();
        builder.AddFormAction().Self().From("https://accounts.google.com");
    })
    .AddPermissionsPolicy(builder => {
        builder.AddAccelerometer().None();
        builder.AddCamera().None();
        builder.AddGeolocation().None();
        builder.AddMicrophone().None();
        builder.AddPayment().None();
        builder.AddUsb().None();
    })
);
```

| Header | Value | Why |
|--------|-------|-----|
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (production only) | Forces HTTPS for 1 year |
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type confusion attacks |
| `X-Frame-Options` | `DENY` (also via CSP `frame-ancestors 'none'`) | Prevents clickjacking |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leak |
| `Permissions-Policy` | All sensitive features denied | Minimum-surface default |
| `Content-Security-Policy` | See below | XSS defense in depth |

### Content Security Policy — Fluent UI 2 Trade-off

`script-src 'self'` — no inline scripts, no `eval`. Vite production builds emit only
external scripts, so this works out of the box.

`style-src 'self' 'unsafe-inline'` — REQUIRED because Fluent UI 2's underlying
**Griffel** runtime injects styles into `<style>` tags at runtime. There is no way to
ship Fluent UI 2 with a strict `style-src` without a nonce pipeline.

A nonce-based CSP for styles IS technically feasible (Vite supports it via
`html-inject-attributes-plugin` + a server-side rendered nonce), but it adds build
complexity, requires server-side templated HTML, and breaks hot-module reload locally.
**Decision for MVP:** accept `'unsafe-inline'` on `style-src` only — `script-src`
stays strict. This is the same CSP posture Microsoft's own Fluent UI demo sites use.

If a future requirement (FedRAMP, government tenant) demands strict `style-src`, the
nonce pipeline becomes future work — not a code rewrite, only a build-config and
middleware change.

### HTTPS Enforcement

```csharp
if (!app.Environment.IsDevelopment()) {
    app.UseHttpsRedirection();
    // HSTS preload header is added via the security headers middleware above
}
```

Development uses HTTP for the API directly (no redirect loop with the Vite dev server).
Production MUST redirect HTTP → HTTPS at the ingress layer AND in the app (defense in
depth — never rely on ingress alone).

### Middleware Order

```csharp
app.UseForwardedHeaders();      // first — see Containerization Readiness
app.UseHttpsRedirection();      // production only
app.UseSecurityHeaders(...);    // BEFORE routing — headers apply to all responses including 404s
app.UseRouting();
app.UseCors("KanbanWebApp");
app.UseRateLimiter();           // AFTER auth — partitions by user
app.UseAuthentication();
app.UseAuthorization();
```

Wrong order means security headers don't apply to error responses — Snyk SAST will flag.

---

## Configuration & Options Pattern

All configuration MUST be accessed through strongly-typed options classes — never
`IConfiguration` directly outside the registration point. This makes configuration shape
explicit, validated at startup, and discoverable.

### Pattern

```csharp
public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    [Required, MinLength(1)] public required string ClientId { get; init; }
    [Required, MinLength(1)] public required string ClientSecret { get; init; }
}

builder.Services
    .AddOptions<GoogleAuthOptions>()
    .Bind(builder.Configuration.GetSection(GoogleAuthOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Rules (Non-Negotiable)

- Every configuration section MUST have a typed options class.
- Every options registration MUST include `.ValidateDataAnnotations().ValidateOnStart()`.
  Missing or malformed configuration MUST fail at boot, NOT at first request. In containers
  this means a misconfigured deploy is caught by readiness-probe failure on rollout — never
  silently degrades to "all requests return 500."
- Services consume `IOptions<T>` (snapshot) or `IOptionsMonitor<T>` (reload on change) via
  constructor injection — never `IConfiguration`.
- Options classes MUST be `sealed`, immutable (`init`-only setters), and use `required` for
  non-optional fields.

### Required Options Classes (MVP)

| Class | Section | Purpose |
|-------|---------|--------|
| `GoogleAuthOptions` | `Authentication:Google` | ClientId, ClientSecret |
| `ConnectionStringOptions` | `ConnectionStrings` | `Kanban` connection string |
| `CorsOptions` | `Cors` | `AllowedOrigins` array |
| `SeedOptions` | `Seed` | `AdminEmail` |

### Configuration Precedence (Unchanged from *Local Secrets Management*)

1. Environment variables (production / CI)
2. User secrets (local development only)
3. `appsettings.Development.json` (non-secret dev defaults)
4. `appsettings.json` (shape documentation + safe defaults)

---

## Local Secrets Management

`WebApplication.CreateBuilder` automatically loads user secrets when
`ASPNETCORE_ENVIRONMENT=Development` — zero extra code required.

### One-Time Setup Per Dev Machine (run in `src/Kanban.Api/`)

```
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId"     "<value>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<value>"
dotnet user-secrets set "ConnectionStrings:Kanban"           "Data Source=kanban.db"
dotnet user-secrets set "Seed:AdminEmail"                    "<value>"
```

### `appsettings.json` — Shape Documentation Only

Documents the expected structure with empty values. Safe to commit — contains no secrets:

```json
{
  "Authentication": { "Google": { "ClientId": "", "ClientSecret": "" } },
  "ConnectionStrings": { "Kanban": "" },
  "Cors": { "AllowedOrigins": [] },
  "Seed": { "AdminEmail": "" }
}
```

### Configuration Precedence (High to Low)

1. Environment variables (production / CI)
2. User secrets (local development only)
3. `appsettings.Development.json` (non-secret dev defaults: log levels, SQLite path template)
4. `appsettings.json` (shape documentation + safe defaults)

**Production:** environment variables or Azure Key Vault via `IConfiguration` providers —
zero code changes from the local pattern.

The `dotnet user-secrets` store is local-dev-only. Containers MUST NEVER bake user secrets
into the image. Production reads configuration exclusively from environment variables or a
secrets manager — see *Configuration & Options Pattern* for the typed-options layer that
consumes them.

### gitleaks Rule

`.gitleaks.toml` MUST include a pattern matching the Google OAuth client secret format
(`GOCSPX-` prefix) to hard-block any accidental commit of credentials.

---

## Code Quality Tooling

### EditorConfig (committed to repo root as `.editorconfig`)

```ini
root = true

[*]
indent_style = space
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
indent_size = 4

[*.{json,js,ts,tsx,jsx,css,scss,html}]
indent_size = 2

[*.{yml,yaml}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

### ESLint + Prettier (frontend — `src/Kanban.Web/`)

- **ESLint** with `@typescript-eslint`, `eslint-plugin-react`, `eslint-plugin-react-hooks`,
  `eslint-plugin-jsx-a11y` (accessibility violations are ESLint *errors*, not warnings).
- **Prettier** for all formatting. `eslint-config-prettier` MUST be the last `extends` entry.
- Both config files MUST be committed to `src/Kanban.Web/`.
- ESLint and Prettier MUST run via `lint-staged` as a pre-commit hook so formatting issues
  never reach the remote.

---

## Stack & Constraints

| Layer | Choice | Notes |
|-------|--------|-------|
| Backend API | ASP.NET 10 minimal API (REST) | .NET 10, nullable enabled, implicit usings |
| Frontend | React (latest stable) | Vite, TypeScript; HTTP/REST transport |
| UI Component Library | Microsoft Fluent UI 2 | `@fluentui/react-components` |
| Server state management | TanStack Query v5 | `@tanstack/react-query`; no Redux/Zustand |
| Drag and drop | dnd-kit | `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities` |
| Error boundaries | react-error-boundary | Board / Lane / Card granularity |
| Persistence (local/CI) | SQLite | Dapper; DbUp migrations |
| Persistence (production) | PostgreSQL | Same Dapper repos; swap at IDbConnection factory |
| Container base image | mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled | Non-root, distroless-style; chiseled is MS-recommended for .NET 10 |
| Health checks | Microsoft.Extensions.Diagnostics.HealthChecks | `/health/live` + `/health/ready`; orchestrator-friendly |
| Configuration validation | Microsoft.Extensions.Options.DataAnnotations | `ValidateDataAnnotations().ValidateOnStart()` — fail at boot |
| Container image scanning | Snyk Container | Same gate as SCA/SAST/IaC — medium+ blocks merge |
| Authentication | Google OAuth 2.0 | ASP.NET OIDC middleware |
| Observability (logs/traces/metrics) | OpenTelemetry + OTLP exporter | Local dev: Aspire Dashboard container |
| HTTP resilience | Microsoft.Extensions.Http.Resilience | Standard handler: timeout + retry + circuit breaker |
| Rate limiting | ASP.NET 10 built-in `AddRateLimiter` | In-memory MVP; Redis-backed for multi-replica |
| Security headers | NetEscapades.AspNetCore.SecurityHeaders | HSTS, CSP (style-src unsafe-inline for Fluent UI) |
| Resilience & retries | Polly | Transaction lock contention + external API transient faults |
| API versioning | Asp.Versioning.Http | URL path segment (`/api/v1/`); APIM-compatible |
| API documentation | Microsoft.AspNetCore.OpenApi + Scalar.AspNetCore | Dev only; APIM-importable |
| Secret detection | gitleaks | Pre-commit hard gate; `.gitleaks.toml` in repo root |
| Security (SAST/SCA/IaC) | Snyk (free tier) | https://app.snyk.io + VS Code extension required |
| Backend code style | EditorConfig | `.editorconfig` at repo root |
| Frontend linting | ESLint + Prettier | `lint-staged` pre-commit hook; a11y errors block commit |
| Backend tests | xUnit + FluentAssertions | `tests/unit/` and `tests/integration/` |
| Frontend component tests | React Testing Library + Vitest | `src/Kanban.Web/tests/` |
| E2E tests | Playwright | `tests/e2e/` |
| CI/CD | GitHub Actions | Required status checks; merge-commit (preserves TDD evidence) |
| Integration DB (CI) | Testcontainers.PostgreSql | Real Postgres per test collection |
| Frontend production serving | nginx-alpine | Separate container; SPA fallback + cache headers |
| Bundle size guard | size-limit (or equivalent) | CI gate: 300KB initial JS gzipped |
| Best practice reference | Microsoft Learn MCP | Query for C# and React patterns |

---

## Token Efficiency & AI Agent Ergonomics

Efficient AI collaboration is a first-class architectural concern. These rules apply to all
contributors — human and AI.

- **CLAUDE.md MUST stay under ~40 lines.** It loads every session. Bloated files cause agents
  to ignore instructions. Link to docs; never inline them.
- **Use `@file` references in prompts** rather than copy-pasting content.
- **One task per session for implementation work.** Use `/clear` between unrelated tasks.
- **Use subagents for investigation.** Research MUST use subagents so exploration does not
  consume the main session's context window.
- **Self-documenting code reduces agent re-reads.** Precise names mean a single file read is
  sufficient. Comments that explain *what* code does force re-reads and burn tokens redundantly.
- **Specs are the source of truth.** Agents MUST read the spec before asking clarifying
  questions.
- **Plan mode before implementation.** Explore and verify approach before any writes. A correct
  plan eliminates costly correction loops.
- **Query Microsoft Learn MCP before proposing patterns.** Don't rely on training data alone
  for C# or React best practices — fetch current guidance.
- Agents SHOULD use `/compact` when a phase completes before starting the next.

---

## Development Workflow

- Every feature starts on a dedicated branch created via `/speckit-git-feature`.
- The gitleaks pre-commit hook MUST be installed before the first commit. Hard prerequisite.
- ESLint + Prettier `lint-staged` hook MUST be installed in `Kanban.Web` before first FE
  commit.
- Commits MUST be made after each completed task.
- A failing-test commit MUST precede the passing-implementation commit (TDD evidence in
  history).
- AI session logs MUST be appended to `SESSION_LOG.md` after each working session.
- All four test layers MUST pass before a feature branch is merged.
- NFR gates (maintainability, usability, performance) MUST be verified before merge.
- Snyk MUST show no medium, high, or critical severity findings before merge.
- CI MUST run on every PR. Merge to `main` requires all required status checks green
  (see *CI/CD Gates*).

---

## CI/CD Gates

Every gate already mandated in this constitution (tests, Snyk, gitleaks, NFR checks)
MUST execute in CI on every pull request. Manual enforcement is not acceptable — gates
that run only on a developer's machine are gates that don't run.

### Platform

GitHub Actions. Free for public repositories, first-class for the .NET ecosystem,
standard tooling for the spec-kit workflow. No self-hosted runners for MVP.

### Pipeline Shape

```
1. checkout + restore caches (NuGet, npm, Playwright browsers)
2. build (dotnet build, npm run build)
3. test — all four layers in parallel where possible:
   - tests/unit/         (xUnit + FluentAssertions)
   - tests/integration/  (xUnit + FluentAssertions + Testcontainers Postgres)
   - src/Kanban.Web/     (RTL + Vitest)
   - tests/e2e/          (Playwright)
4. security:
   - Snyk SCA, SAST, License
   - gitleaks (secret scan on diff)
5. build container image (multi-stage Dockerfile)
6. Snyk Container scan of the built image
7. publish image to GHCR (on main only)
```

### Required Status Checks

The following MUST be required on `main` via GitHub branch protection — not just
convention:

- `build`
- `test/unit`, `test/integration`, `test/component`, `test/e2e`
- `snyk/sca`, `snyk/sast`, `snyk/license`, `snyk/container`
- `gitleaks`

No PR merges to `main` without all required checks green. Force-pushes to `main` are
disabled at the branch protection level.

### Image Tag Strategy

| Tag | Purpose | Mutable? |
|-----|---------|----------|
| `sha-<commit-sha>` | Deploy target — immutable, reproducible | No |
| `latest` | Convenience pointer to most recent `main` build | Yes |
| `v<major>.<minor>.<patch>` | Release tag (semver) | No |

Production deploys MUST reference the immutable `sha-<commit>` tag.

### Branching Strategy

- Trunk-based development with short-lived feature branches (created via
  `/speckit-git-feature`).
- Direct pushes to `main` are disabled at branch protection.
- All changes reach `main` via PR.
- **Merge strategy: merge-commit** (not squash). Principle II mandates that the
  Red-Green-Refactor cycle MUST be verifiable in commit history. Squash merges erase
  the failing-test commit that precedes the passing-implementation commit. Merge
  commits preserve TDD evidence at the cost of slightly noisier `main` history — the
  trade-off favors auditability.

### PR Template Requirements

`.github/PULL_REQUEST_TEMPLATE.md` MUST require:

- Linked spec (`specs/<branch>/spec.md`)
- Test plan checkbox confirming all four layers covered
- NFR confirmation (performance, accessibility, maintainability)
- Snyk green confirmation
- Screenshots / screen recordings for UI changes
- Migration considerations (any DB schema change requires explicit call-out)

### Concurrency

```yaml
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true
```

Cancel in-progress runs on new pushes to the same PR. Saves CI minutes; latest commit
is always what matters.

### Test Artifacts on Failure

- **Playwright**: configure `trace: 'on-first-retry'` and `video: 'retain-on-failure'`.
  CI MUST upload `playwright-report/` and `test-results/` as workflow artifacts on
  failure with 14-day retention.
- **dotnet test**: `--logger "trx;LogFileName=test-results.trx"` results uploaded on
  failure for diagnostic review.

### Failure Triage Rule

A red CI is not a "flaky test" until proven so. The default response to a failed check
is to investigate the failure, not re-run the job. Re-runs are permitted only after a
documented diagnosis. Repeated flakiness MUST be tracked and resolved.

---

## Integration Tests — Testcontainers in CI

Integration tests already use a real database (Principle II — no mocking infrastructure).
The remaining gap: local development uses SQLite for fast feedback, but production runs
on Postgres. Postgres-specific behavior (type coercion, transaction isolation, lock
contention) MUST be exercised before staging.

### Strategy

- **Local developer machine**: integration tests run against SQLite. Fast (~seconds),
  no Docker required for the test-on-save loop.
- **CI**: integration tests run against **both** SQLite AND Postgres via Testcontainers.
  Postgres path catches engine-specific divergence before staging.

### Package

`Testcontainers.PostgreSql` — official Testcontainers .NET package. Spins up a real
Postgres container, returns a connection string, tears down on dispose.

### Pattern — xUnit IAsyncLifetime

```csharp
public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } =
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("kanban_test")
            .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        // Run DbUp migrations against the fresh container
    }

    public async Task DisposeAsync() => await Container.DisposeAsync();
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture> { }
```

### Rules

- The same integration test suite MUST pass against SQLite AND Postgres. Engine-specific
  test forks are forbidden — a test that doesn't work on both engines indicates a
  portability bug (see *Database Portability*).
- CI matrix builds run both database engines in parallel.
- Local developers MAY skip the Postgres run (Docker required); CI MUST run both.
- Container lifecycle is per-collection (one Postgres container per test collection,
  not per test) — keeps wall time reasonable.
- Migrations MUST run against the freshly-started container in the fixture's
  `InitializeAsync`. This validates migrations work end-to-end against a real Postgres,
  not just SQLite.

### Prerequisite

Docker Desktop (or Docker Engine on Linux) MUST be running for Testcontainers to work
locally. CI runners (`ubuntu-latest`) have Docker available by default.

### Test Infrastructure Isolation

`WebApplicationFactory` configures the API for in-process testing. Any middleware that shapes
responses — particularly rate limiters — MUST be neutralized so tests verify DB and business
logic, not infrastructure limits.

**Rate limiter override (non-negotiable):**

```csharp
// In KanbanWebAppFactory.ConfigureServices
services.RemoveAll(typeof(IConfigureOptions<RateLimiterOptions>));
services.AddRateLimiter(opts => {
    // Every named policy defined in Program.cs MUST appear here.
    // A missing policy causes a runtime error on any endpoint using RequireRateLimiting.
    opts.AddFixedWindowLimiter("anonymous",       o => { o.PermitLimit = 10_000; ... });
    opts.AddSlidingWindowLimiter("authenticated", o => { o.PermitLimit = 10_000; ... });
    opts.AddSlidingWindowLimiter("mutating",      o => { o.PermitLimit = 10_000; ... });
});
```

When a new rate-limiting policy is added to `Program.cs`, the factory override MUST be updated
in the same commit. A policy present in production but absent from the factory causes a
`KeyNotFoundException` at test startup — caught immediately.

**Concurrency test load calibration:**

Concurrency tests verify a DB invariant (e.g. exactly one winner). They do NOT test throughput.
Load MUST be calibrated to the target test environment:

- SQLite local tests: 3–5 concurrent requests — sufficient to trigger a race condition without
  exhausting SQLite's single-writer constraint and requiring excessive Polly retries.
- Each concurrency test MUST include a comment naming the invariant being verified.
- Load testing (higher request volumes) belongs in a dedicated suite that explicitly documents
  its infrastructure requirements.

**Polly retry calibration:**

Retry counts MUST be proportional to expected concurrency. For SQLite with 3–5 concurrent
writers and WAL mode + `busy_timeout` configured, 5 retries with exponential backoff and
jitter is the correct default. `MaxRetryAttempts = 20` is a symptom of over-loaded tests,
not a production tuning requirement.

---

## Frontend Build & Deploy Model

The frontend deployment model is decided now — the choice between same-image and
separate-image hosting has scaling and ops implications that are awkward to reverse.

### Production: Separate Static Container

The built SPA ships as a **separate container** serving static files via nginx-alpine.

```
┌──────────────┐         ┌──────────────────────┐
│  Ingress     │ ──/───▶ │  Kanban.Web (nginx)  │  static files
│  (LB / APIM) │         └──────────────────────┘
│              │
│              │ ──/api──▶ ┌──────────────────────┐
│              │           │  Kanban.Api (.NET)   │  REST API
└──────────────┘           └──────────────────────┘
```

Why separate:

- **Independent scaling** — UI traffic and API traffic scale differently. Same-image
  couples them artificially.
- **CDN-friendly** — static assets cache at the edge without API coupling.
- **Smaller blast radius** — frontend deploys don't restart API replicas.
- **Cache headers diverge** — long-cache for hashed assets, no-cache for `index.html`.
  Mixing them in an ASP.NET `UseStaticFiles` pipeline requires hand-rolled middleware.

### Production Image — nginx-alpine

```dockerfile
# Stage 1: build
FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 2: serve
FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 8080
USER nginx
```

nginx config requirements (`src/Kanban.Web/nginx.conf`):

- **SPA fallback**: `try_files $uri $uri/ /index.html;` — React Router needs all
  unmatched paths to return `index.html`.
- **Cache headers**: hashed assets (`assets/*.js`, `assets/*.css`) get
  `Cache-Control: public, max-age=31536000, immutable`. `index.html` gets
  `Cache-Control: no-cache`.
- **Listens on 8080** (non-root) — same convention as the API container.
- **Security headers** for static assets — the same CSP applies, served by nginx
  for the static origin.

### Local Dev: Vite Dev Server

Unchanged from current setup — `npm run dev` starts Vite on `http://localhost:5173`
with proxy to the API on `http://localhost:5077`. nginx is NOT used locally.

### Rejected: Single-Image (`UseStaticFiles` in Kanban.Api)

Explicitly considered and rejected for production:

- Couples API and UI scaling.
- Hand-rolled cache header middleware.
- API restarts on UI-only changes.
- Doesn't match the production topology where the static origin is typically CDN-fronted.

Listing this rejection in the constitution prevents the design from being re-litigated
on every onboarding.

### Vite Production Build Configuration

- **Source maps**: emit but DO NOT ship `.map` files in the production image. Configure
  `build.sourcemap: 'hidden'` — generates maps without the `//# sourceMappingURL`
  comment. Maps are uploaded separately to the observability backend for stack-trace
  symbolication.
- **Code splitting**: route-level `React.lazy()` for the Board page and any future
  top-level routes — keeps the initial bundle below the budget.
- **Minification**: default Vite/esbuild — no custom config required.

### Bundle Size Budget (Enforced in CI)

| Asset | Budget (gzipped) |
|-------|-----------------|
| Initial JS (entry chunk + sync deps) | ≤ 300 KB |
| Initial CSS | ≤ 50 KB |
| Total per-route lazy chunk | ≤ 200 KB |

CI MUST run a bundle-size check (`size-limit` or equivalent) after `npm run build`.
Budget overages block merge — same severity as a failed test. New dependencies that
push over the budget require justification in the plan's Complexity Tracking table.

---

## Test Data Builders

Domain entities evolve. Tests that construct entities inline churn every time a
constructor parameter is added. Test data builders insulate tests from entity shape
changes and document the meaningful values for each test.

### Pattern — Object Mother + Builder

Location: `tests/unit/Builders/` (also accessible to `tests/integration/`).

```csharp
public sealed class CardBuilder
{
    private string _title = "Default Card Title";
    private Guid _laneId = Guid.NewGuid();
    private int _position = 0;
    private string? _description;
    private DateOnly? _dueDate;

    public static CardBuilder ACard() => new();

    public CardBuilder WithTitle(string title) { _title = title; return this; }
    public CardBuilder InLane(Guid laneId) { _laneId = laneId; return this; }
    public CardBuilder AtPosition(int position) { _position = position; return this; }
    public CardBuilder WithDescription(string description)
        { _description = description; return this; }
    public CardBuilder DueOn(DateOnly date) { _dueDate = date; return this; }

    public Card Build() => new(_title, _laneId, _position, _description, _dueDate);
}
```

Usage:

```csharp
var card = ACard().WithTitle("Ship MVP").AtPosition(3).Build();
```

### Rules

- One builder per aggregate root and per value object: `BoardBuilder`, `LaneBuilder`,
  `CardBuilder`, `UserBuilder`, etc.
- Defaults MUST produce a valid entity — calling `.Build()` with no setters returns
  something that passes domain invariants.
- Builders return the concrete entity type, not the builder, on `.Build()`.
- New tests MUST use builders. Existing tests are grandfathered but SHOULD migrate
  opportunistically.
- Builders live in test projects only — they MUST NOT be referenced from production
  code. If production code wants a similar factory, it gets its own Factory in
  `Kanban.Business` per Principle VII.

---

## Developer Onboarding

This section governs the required setup that agents and developers MUST complete before any
implementation work begins. It complements the README and does not replace it.

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- Git + gitleaks (`brew install gitleaks` / `winget install gitleaks`)
- Snyk VS Code extension (required by Principle IV)
- Docker Desktop (or Docker Engine on Linux) — required for Testcontainers integration
  tests and the local Aspire Dashboard
- Google Cloud project with an OAuth 2.0 Web Application credential

### First-Run Setup Sequence (Order Matters)

```
1. git clone <repo>
2. Install gitleaks pre-commit hook (script provided at repo root)
3. cd src/Kanban.Web && npm install        # also installs lint-staged hook
4. In src/Kanban.Api/:
   dotnet user-secrets init
   dotnet user-secrets set "Authentication:Google:ClientId"     "<value>"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "<value>"
   dotnet user-secrets set "ConnectionStrings:Kanban"           "Data Source=kanban.db"
   dotnet user-secrets set "Seed:AdminEmail"                    "<your-email>"
5. dotnet run --project src/Kanban.Api -- migrate   # DbUp migrations + seeds admin user
```

### Google Cloud Console Setup

- Enable Google Identity API
- Authorised redirect URI: `https://localhost:7282/signin-google`
- Client ID and secret → user secrets only, **never** `appsettings.json`

### Port Map

| Service | URL |
|---------|-----|
| API (HTTP) | http://localhost:5077 |
| API (HTTPS) | https://localhost:7282 |
| Vite dev server | http://localhost:5173 |
| Scalar API docs | http://localhost:5077/scalar/v1 |

### Running Tests

```
dotnet test tests/unit/           # xUnit + FluentAssertions unit tests
dotnet test tests/integration/    # xUnit + FluentAssertions, real SQLite
cd src/Kanban.Web && npm test     # RTL component tests
npx playwright test               # e2e — requires API + Web both running
```

### Verified Onboarding Checklist

Agents MUST confirm all items before starting any implementation task:

- [ ] gitleaks hook installed and verified to block a test secret commit
- [ ] lint-staged hook fires on frontend file changes
- [ ] `dotnet user-secrets list` shows all four secrets set
- [ ] DB migrated and admin user seeded
- [ ] All four test layers pass on a clean checkout

---

## MVP Implementation Order

This constitution defines **architectural targets**, not a day-one build list. The
sections here describe a production-ready end state. The MVP scopes which targets ship
in v1, which are wired as additive seams for future work, and which are deferred
entirely.

### MVP Delivery Scope

The MVP MUST demonstrate:

- Working locally on a single developer machine
- All four test layers passing (unit, integration, RTL component, Playwright e2e)
- All Principles I–IX satisfied
- Recordable end-to-end demo

The MVP MUST NOT include:

- Production deployment
- Multi-replica operation
- External infrastructure dependencies beyond Google OAuth

### Section Scoping

| Scope | Sections |
|-------|----------|
| **(a) Implement now** — required for the four-test-layer local demo | Principles I–IX, Solution Structure, DDD Constraints, Domain Model, Authorization Model, Transaction Pattern, Database Portability (SQLite path only), Error Handling (including `Verify` and frontend boundaries), Frontend State Management, Drag and Drop, CORS, API Versioning (v1 only), OpenAPI (Scalar dev UI), Logging (ILogger + correlation ID), Local Secrets Management, Configuration & Options Pattern, Code Quality Tooling, Developer Onboarding, Test Data Builders, HTTP Resilience, Security Headers, Health & Lifecycle endpoints, CI/CD Gates (tests + Snyk + gitleaks only; no container publish) |
| **(b) Wire the seam, stub the implementation** — architectural decision recorded, no infrastructure built | Multi-Tenancy Readiness (no `TenantContext` instance); OpenTelemetry exporter (registered, OTLP endpoint env-var driven, no required local backend); Rate Limiting (registered with permissive limits, no stress testing); Containerization Readiness (no Dockerfile authored); Testcontainers Postgres (CI matrix optional, SQLite-only CI acceptable); Frontend Build & Deploy Model (Vite dev server is the only deployment path); Bundle Size Budget (target documented, no CI enforcement) |
| **(c) Defer entirely** — promote when deployment work begins | Distributed Rate Limiting; Postgres production runtime; APIM integration; `openapi-typescript` client generation; nonce-based CSP; container deploy; nginx-alpine production image; Snyk Container scanning; GHCR image publish; HSTS preload; multi-replica migration coordination |

### Rules

- The first feature spec's Constitution Check section MUST reference this scoping
  table. Any section the feature promotes from (b) or (c) to (a) MUST be called out.
- Promoting a section into MVP scope is always permitted but requires a justification
  in the plan's Complexity Tracking table — extra scope MUST be a deliberate decision.
- Demoting a section out of (a) is NOT permitted once the spec is approved — if the
  work is too large, the feature scope shrinks, not the constitutional floor.
- This section MUST be updated when deployment work begins. The constitution itself
  doesn't change; only the scoping does.

---

## Governance

This constitution supersedes all other development practices and informal conventions.
Amendments require: (1) documented rationale, (2) version bump per the policy below,
(3) update to this file.

- **MAJOR**: Removal or redefinition of a core principle.
- **MINOR**: Addition of a new principle or material expansion of an existing one.
- **PATCH**: Clarifications, wording fixes, or non-semantic refinements.

All implementation tasks and AI agent outputs MUST be verified for compliance with this
constitution before a task is marked complete.

**Version**: 1.7.7 | **Ratified**: 2026-05-23 | **Last Amended**: 2026-06-01
