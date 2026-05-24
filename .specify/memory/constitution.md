<!--
SYNC IMPACT REPORT
Version: 1.2.0 → 1.3.0 (MINOR — REST replaces gRPC; linting toolchain added; database
  portability rules added; SQL injection rules made explicit; domain model concepts added;
  multi-tenancy readiness seams documented; SQLite-grounded transaction pattern + Polly retries added)
Modified Principles:
  - II. TDD — "gRPC endpoints" → "REST endpoints" in integration test row
  - IV. Security-First — explicit SQL injection prevention rules added
  - VI. Self-Documenting Code — "gRPC service contracts" → "REST API contracts"
  - VII. Design Patterns — gRPC handler row → REST minimal API endpoint row
  - IX. NFRs / Performance — "gRPC response time" → "REST API response time"
Added Sections:
  - Transaction Pattern (IDbTransaction, deferred, savepoints, Polly retries — per MS Learn SQLite docs)
  - Code Quality Tooling (EditorConfig, ESLint + Prettier)
  - Database Portability (SQLite ↔ PostgreSQL swap strategy)
  - Domain Model Concepts
  - Multi-Tenancy Readiness (seam decisions; not in scope for MVP)
Removed Sections: none
Templates:
  ✅ .specify/templates/plan-template.md — REST, linting, transaction pattern, Polly alignment updated
  ✅ .specify/templates/tasks-template.md — linting setup + transaction/Polly tasks required
  ✅ .specify/templates/spec-template.md — no change required
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
| Integration | xUnit + FluentAssertions | `tests/integration/` | REST endpoints + real SQLite (no mocks for infra) |
| Component | React Testing Library + Jest | `src/Kanban.Web/tests/` | Fluent UI component rendering, user interactions |
| E2E | Playwright | `tests/e2e/` | All user scenarios from the feature spec |

**Fluent UI component test tiers** (decision tree — use the lightest sufficient tier):

1. Logic/utility functions testable in isolation → Jest unit test
2. Requires component rendering without a browser → React Testing Library + Jest
3. Requires actual browser behavior → Playwright e2e

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
- All developers MUST have the **Snyk VS Code extension** installed and active.
- Snyk-flagged medium, high, or critical severity findings block merge.

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
| **User** | Authenticated via Google OAuth; has identity, display name, avatar |
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
  external data before opening the transaction.
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
| Persistence (local/CI) | SQLite | Dapper; DbUp migrations |
| Persistence (production) | PostgreSQL | Same Dapper repos; swap at IDbConnection factory |
| Authentication | Google OAuth 2.0 | ASP.NET OIDC middleware |
| Resilience & retries | Polly | Transaction lock contention + external API transient faults |
| Secret detection | gitleaks | Pre-commit hard gate; `.gitleaks.toml` in repo root |
| Security (SAST/SCA/IaC) | Snyk (free tier) | https://app.snyk.io + VS Code extension required |
| Backend code style | EditorConfig | `.editorconfig` at repo root |
| Frontend linting | ESLint + Prettier | `lint-staged` pre-commit hook; a11y errors block commit |
| Backend tests | xUnit + FluentAssertions | `tests/unit/` and `tests/integration/` |
| Frontend component tests | React Testing Library + Jest | `src/Kanban.Web/tests/` |
| E2E tests | Playwright | `tests/e2e/` |
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
- **Specs are the source of truth.** Agents MUST read the spec before asking clarifying questions.
- **Plan mode before implementation.** Explore and verify approach before any writes. A correct
  plan eliminates costly correction loops.
- **Query Microsoft Learn MCP before proposing patterns.** Don't rely on training data alone
  for C# or React best practices — fetch current guidance.
- Agents SHOULD use `/compact` when a phase completes before starting the next.

---

## Development Workflow

- Every feature starts on a dedicated branch created via `/speckit-git-feature`.
- The gitleaks pre-commit hook MUST be installed before the first commit. Hard prerequisite.
- ESLint + Prettier `lint-staged` hook MUST be installed in `Kanban.Web` before first FE commit.
- Commits MUST be made after each completed task.
- A failing-test commit MUST precede the passing-implementation commit (TDD evidence in history).
- AI session logs MUST be appended to `SESSION_LOG.md` after each working session.
- All four test layers MUST pass before a feature branch is merged.
- NFR gates (maintainability, usability, performance) MUST be verified before merge.
- Snyk MUST show no medium, high, or critical severity findings before merge.

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

**Version**: 1.3.0 | **Ratified**: 2026-05-23 | **Last Amended**: 2026-05-24
