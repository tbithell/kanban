# Tasks: Authentication and User Onboarding

**Input**: Design documents from `specs/001-auth-onboarding/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓

**Tests**: TDD is mandatory per constitution. All test tasks MUST be written first and MUST
fail (red commit) before the implementation tasks (green commit) that follow them.

**Organization**: Tasks are grouped by user story to enable independent implementation and
testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no shared dependency on incomplete work)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in every description

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create solution, project scaffold, and tooling configuration. No business logic.

- [x] T001 Solution `Kanban.slnx` and one project already exist (`Kanban/Kanban.csproj` — default ASP.NET template). Restructure: move `Kanban/` → `src/Kanban.Api/` and rename csproj to `Kanban.Api.csproj`; update `Kanban.slnx` to reference new path. Then create the remaining 6 backend projects (Kanban.Business, Kanban.Domain, Kanban.Contracts, Kanban.AntiCorruption, Kanban.Data, Kanban.DataAccess) under `src/` and add all to `Kanban.slnx`; scaffold `src/Kanban.Web/` as a Vite TypeScript React app; add all project-to-project references per the dependency matrix in plan.md
- [x] T002 Add all NuGet packages to backend projects per plan.md Technical Context: Dapper, DbUp-SQLite, FluentValidation.AspNetCore, Polly, Microsoft.Extensions.Http.Resilience, NetEscapades.AspNetCore.SecurityHeaders, Asp.Versioning.Http, Microsoft.AspNetCore.OpenApi, Scalar.AspNetCore, Microsoft.Extensions.Diagnostics.HealthChecks, Microsoft.AspNetCore.Authentication.Google in src/Kanban.*/
- [x] T003 [P] Create appsettings.json (empty-value shape: Authentication, ConnectionStrings, Cors, Seed sections) and appsettings.Development.json (Logging levels per constitution) in src/Kanban.Api/
- [x] T004 [P] Commit .editorconfig to repo root with constitution spec (indent_style=space, C#=4, web=2, LF, UTF-8, trim_trailing_whitespace=true, insert_final_newline=true)
- [x] T005 Install frontend npm packages in src/Kanban.Web/: @fluentui/react-components, @tanstack/react-query, @tanstack/react-query-devtools, react-error-boundary, react-router-dom; verify Vite TypeScript template is in place
- [x] T006 [P] Create ESLint config (src/Kanban.Web/.eslintrc.cjs) with @typescript-eslint, eslint-plugin-react, eslint-plugin-react-hooks, eslint-plugin-jsx-a11y (a11y as errors), eslint-config-prettier last; create src/Kanban.Web/.prettierrc
- [x] T007 Configure lint-staged pre-commit hook in src/Kanban.Web/package.json to run ESLint + Prettier on staged frontend files
- [x] T008 Install gitleaks pre-commit hook via scripts/install-hooks.sh at repo root; verify it blocks a commit containing a test secret (GOCSPX- prefix)
- [x] T009 Create xUnit test projects tests/unit/ (references Kanban.Domain, Kanban.Business) and tests/integration/ (references Kanban.Api, Kanban.DataAccess) with xunit, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Microsoft.Data.Sqlite NuGet packages
- [x] T010 [P] Create Playwright e2e project tests/e2e/ with playwright.config.ts pointing to http://localhost:5173; install @playwright/test browsers
- [x] T011 [P] Create RTL/Jest setup in src/Kanban.Web/: install @testing-library/react, @testing-library/jest-dom, @testing-library/user-event, msw, jest, jest-environment-jsdom, ts-jest; create jest.config.ts and src/Kanban.Web/src/setupTests.ts

**Checkpoint**: Solution builds. All project references resolve. `dotnet build` and `npm run build` succeed with no errors.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain types, DB schema, repositories, and API infrastructure that every user story depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Domain Foundation

- [X] T012 Write failing unit tests for Verify.That<T>: IsNotNull (throws ArgumentNullException), IsNotDefault (throws ArgumentException for Guid.Empty and 0), IsNotEmpty (string), HasMaxLength, IsPositive, IsNonNegative, IsGreaterThan, IsInRange, IsNotEmpty (IEnumerable) in tests/unit/Domain/VerifyTests.cs — **RED commit**
- [X] T013 Implement Verify.cs with static Verify.That<T>([CallerArgumentExpression] string paramName), ParameterVerifier<T> with IsNotNull/IsNotDefault, and all type-specific extension methods in src/Kanban.Domain/Verify.cs — **GREEN commit for T012**
- [X] T014 [P] Write failing unit tests for exception hierarchy (NotFoundException, ForbiddenException, ConflictException, BusinessRuleException, DataAccessException, ExternalServiceException — verify Code property and inheritance chain) in tests/unit/Domain/ExceptionTests.cs — **RED commit**
- [X] T015 [P] Implement KanbanException abstract base → DomainException (NotFoundException, ForbiddenException, ConflictException, BusinessRuleException) and InfrastructureException (DataAccessException, ExternalServiceException) with Code string property in src/Kanban.Domain/Exceptions/ — **GREEN commit for T014**
- [X] T016 [P] Create SystemRole (Admin, Standard) and AuthEventType (SignIn, SignOut, InvitationIssued, InvitationAccepted, AcceptanceRefused) enums in src/Kanban.Domain/Enums/SystemRole.cs and src/Kanban.Domain/Enums/AuthEventType.cs
- [X] T017 [P] Write failing unit tests for User entity (Verify guards throw on null/empty/default args; LinkGoogleIdentity sets GoogleSub; RecordSignIn sets LastSignInAt) in tests/unit/Domain/UserTests.cs — **RED commit**
- [X] T018 [P] Write failing unit tests for Invitation aggregate (IsExpired, IsConsumed, IsRedeemable computed properties; EmailMatches case-insensitive comparison; Consume sets consumed fields) in tests/unit/Domain/InvitationTests.cs — **RED commit**
- [X] T019 [P] Write failing unit tests for InvitationToken value object (Generate produces 43-char URL-safe base64; HashRaw produces consistent lowercase hex; two Generate calls produce different tokens; HashRaw of same input always equal) in tests/unit/Domain/InvitationTokenTests.cs — **RED commit**
- [X] T020 Implement User aggregate root with Verify.That guards in constructor, LinkGoogleIdentity(string googleSub), RecordSignIn(DateTimeOffset) in src/Kanban.Domain/Entities/User.cs — **GREEN commit for T017**
- [X] T021 [P] Implement Invitation aggregate root with IsExpired/IsConsumed/IsRedeemable/EmailMatches/Consume in src/Kanban.Domain/Entities/Invitation.cs — **GREEN commit for T018**
- [X] T022 [P] Implement InvitationToken value object with Generate() (RandomNumberGenerator.GetBytes(32) → URL-safe base64) and HashRaw() (SHA256 hex) in src/Kanban.Domain/ValueObjects/InvitationToken.cs — **GREEN commit for T019**
- [X] T023 [P] Implement AuthEvent record (Id, OccurredAt, EventType, UserId?, Outcome) in src/Kanban.Domain/Events/AuthEvent.cs

### Database

- [X] T024 Create SQLite migration 001_initial_schema.sql (users, invitations, auth_events tables with UNIQUE constraints and indexes per data-model.md schema) in src/Kanban.Data/migrations/sqlite/001_initial_schema.sql
- [X] T025 [P] Create SQLite migration 002_seed_admin.sql with DbUp variable substitution ($AdminEmail$, $AdminUserId$, $SeedTimestamp$) in src/Kanban.Data/migrations/sqlite/002_seed_admin.sql
- [X] T026 [P] Create Postgres variants of both migrations (INSERT OR IGNORE → INSERT INTO ... ON CONFLICT DO NOTHING) in src/Kanban.Data/migrations/postgres/

### Data Access

- [X] T027 Define IUserRepository, IInvitationRepository, IAuthEventRepository interfaces per data-model.md Repository Interfaces section in src/Kanban.DataAccess/Interfaces/
- [X] T028 [P] Write failing integration tests for UserRepository (FindByGoogleSub, FindByEmail, FindById, Insert, LinkGoogleSub, UpdateLastSignIn — all against real SQLite via SqliteTestFixture) in tests/integration/DataAccess/UserRepositoryTests.cs — **RED commit**
- [X] T029 [P] Write failing integration tests for InvitationRepository (FindByTokenHash, FindActiveByEmail, Insert, TryConsumeAsync returns true first call and false on second call with same token) in tests/integration/DataAccess/InvitationRepositoryTests.cs — **RED commit**
- [X] T030 Create SqliteTestFixture implementing IAsyncLifetime (runs DbUp migrations against Microsoft.Data.Sqlite in-memory DB, exposes IDbConnection) in tests/integration/Infrastructure/SqliteTestFixture.cs and [CollectionDefinition] in tests/integration/Infrastructure/SqliteCollection.cs
- [X] T031 Implement UserRepository using Dapper with parameterized @paramName queries; all write methods accept IDbTransaction; no string interpolation in SQL in src/Kanban.DataAccess/Repositories/UserRepository.cs — **GREEN commit for T028**
- [X] T032 [P] Implement InvitationRepository with Dapper; TryConsumeAsync executes the single UPDATE with consumed_at IS NULL guard and returns rowsAffected == 1 in src/Kanban.DataAccess/Repositories/InvitationRepository.cs — **GREEN commit for T029**
- [X] T033 [P] Implement AuthEventRepository with Dapper (Insert only — audit append log) in src/Kanban.DataAccess/Repositories/AuthEventRepository.cs

### API Infrastructure

- [X] T034 Create sealed options classes with [Required] init-only setters and const SectionName: GoogleAuthOptions, SeedOptions, CorsOptions, ConnectionStringOptions in src/Kanban.Api/Options/
- [X] T035 Write Program.cs infrastructure: DbUp runner (DeployChanges.To.SQLiteDatabase, WithScriptsFromEmbeddedResources, WithVariables for seed), SQLite IDbConnection factory DI, all options registrations with .ValidateDataAnnotations().ValidateOnStart(), AddProblemDetails, health checks (/health/live process-only + /health/ready DB check), CORS named policy "KanbanWebApp" from CorsOptions, API versioning v1 group (RequireAuthorization registered later), OpenAPI + Scalar behind IsDevelopment guard, AddRateLimiter (permissive defaults: anonymous 10/min, authenticated 100/min, mutating 30/min), correlation ID middleware in src/Kanban.Api/Program.cs
- [X] T036 Implement DomainExceptionHandler (maps NotFoundException→404, ForbiddenException→403/404, ConflictException→409, BusinessRuleException→422 with code + traceId), InfrastructureExceptionHandler (DataAccessException→500, ExternalServiceException→502), FallbackExceptionHandler (logs Error; returns only title + traceId) in src/Kanban.Api/ErrorHandling/
- [X] T037 Confirm mandatory middleware order in Program.cs: UseForwardedHeaders → UseHttpsRedirection (prod only) → UseSecurityHeaders → UseRouting → UseCors("KanbanWebApp") → UseRateLimiter → UseAuthentication → UseAuthorization

### Test Builders & Frontend Setup

- [X] T038 [P] Create UserBuilder (AUser() factory, WithEmail/WithDisplayName/WithSystemRole/WithGoogleSub/AsAdmin setters, default produces valid User) in tests/unit/Builders/UserBuilder.cs
- [X] T039 [P] Create InvitationBuilder (AnInvitation() factory, ForEmail/WithExpiry/AsExpired/AsConsumed setters, default produces valid unexpired unconsumed Invitation with generated token hash) in tests/unit/Builders/InvitationBuilder.cs
- [X] T040 Set up TanStack QueryClient in src/Kanban.Web/src/main.tsx (staleTime: 30_000, retry: never 4xx, 2x for 5xx/network), wrap <App /> with QueryClientProvider, render ReactQueryDevtools in development; create createQueryClientWrapper() test utility in src/Kanban.Web/src/tests/utils/queryClientWrapper.tsx

**Checkpoint**: Foundation ready. `dotnet test tests/unit/` and `dotnet test tests/integration/` pass. App starts with `dotnet run` — DbUp runs migrations, admin record seeded, health endpoints respond.

---

## Phase 3: User Story 1 — Seeded Admin Signs In (Priority: P1) 🎯 MVP

**Goal**: Admin can sign in via Google and be recognized as the system administrator. Any
Google account that is not the seeded admin and has not been invited is refused with a
"not registered" message — not "sign-in failed".

**Independent Test** (from spec.md US1): Configure admin email, start system, sign in with
the matching Google account, confirm admin lands on a signed-in page. Separately, sign in
with a non-admin, non-invited Google account and confirm "not registered" message.

### Tests for US1 — Write FIRST, ensure they FAIL ⚠️

- [X] T041 [P] [US1] Write failing unit tests for AuthService: (1) admin first sign-in — FindByGoogleSub null, FindByEmail finds User with null GoogleSub → LinkGoogleIdentity + claims added; (2) returning user — FindByGoogleSub finds User → claims added, no link step; (3) unregistered user — both queries return null → no user_id/system_role claims added to principal; (4) UpdateLastSignIn and AuthEvent.SignIn logged in all registered-user paths in tests/unit/Business/AuthServiceTests.cs — **RED commit**
- [X] T042 [P] [US1] Write failing integration tests using WebApplicationFactory: GET /api/v1/auth/me with authenticated registered admin cookie → 200 with systemRole:admin; GET /api/v1/auth/me with authenticated but unregistered cookie → 403 code:user.not_registered; GET /api/v1/auth/me unauthenticated → 401; POST /api/v1/auth/signout → 204 + cookie cleared in tests/integration/Api/AuthEndpointTests.cs — **RED commit**
- [X] T043 [P] [US1] Write failing RTL tests for SignInPage: renders Fluent UI Button with accessible name "Sign in with Google"; button href contains /api/v1/auth/signin in src/Kanban.Web/tests/components/SignInPage.test.tsx — **RED commit**
- [X] T044 [P] [US1] Write failing RTL tests for NotRegisteredPage: renders message "You are not registered" with no application data or navigation visible in src/Kanban.Web/tests/components/NotRegisteredPage.test.tsx — **RED commit**
- [X] T045 [P] [US1] Write failing Playwright e2e tests for US1 acceptance scenarios 1–4: (1) admin email signs in → signed-in landing page; (2) second sign-in for admin → recognized without re-linking; (3) non-invited Google account → "not registered" message, no app data; (4) unauthenticated access to / → redirected to sign-in in tests/e2e/Auth/AdminSignInTests.spec.ts — **RED commit**

### Implementation for US1

- [X] T046 [US1] Implement GoogleIdentityAdapter (extracts sub claim, email claim, display_name claim from ClaimsPrincipal with Verify.That guards; returns GoogleIdentity value record) in src/Kanban.AntiCorruption/Adapters/GoogleIdentityAdapter.cs
- [X] T047 [US1] Implement IAuthService interface and AuthService (HandleTicketReceivedAsync: calls GoogleIdentityAdapter → FindByGoogleSub → if null: FindByEmail + LinkGoogleIdentity for admin; if still null: return without adding claims; else: AddClaim user_id + system_role, UpdateLastSignIn, RecordAuthEvent SignIn) in src/Kanban.Business/Services/AuthService.cs — **GREEN commit for T041**
- [X] T048 [US1] Implement ICurrentUserService and CurrentUserService (UserId from user_id claim, SystemRole from system_role claim, IsAuthenticated from identity) in src/Kanban.Api/Auth/CurrentUserService.cs
- [X] T049 [US1] Implement RegisteredUserRequirement (marker IAuthorizationRequirement) and RegisteredUserHandler (checks user_id claim present → success; absent → context.Fail(); handler returns Forbid() producing 403 Problem Details code:user.not_registered) in src/Kanban.Api/Auth/RegisteredUserRequirement.cs and RegisteredUserHandler.cs
- [X] T050 [US1] Wire Google OIDC in Program.cs: AddAuthentication (Cookie + Google schemes), AddGoogle with ClientId/ClientSecret from GoogleAuthOptions, OnTicketReceived calling IAuthService.HandleTicketReceivedAsync, AddAuthorizationBuilder with "RegisteredUser" policy (RegisteredUserRequirement) and "Admin" policy (RequireClaim system_role:admin); register ICurrentUserService scoped, IAuthService scoped, GoogleIdentityAdapter scoped in src/Kanban.Api/Program.cs
- [X] T051 [US1] Implement AuthEndpoints: GET /api/v1/auth/me (RequireAuthorization("RegisteredUser") → return CurrentUserDto via IAuthService.GetCurrentUserAsync); GET /api/v1/auth/signin (no auth required → Challenge(GoogleDefaults, returnUrl from CorsOptions + query param)); POST /api/v1/auth/signout (RequireAuthorization any → SignOutAsync + 204) in src/Kanban.Api/Endpoints/AuthEndpoints.cs — **GREEN commit for T042**
- [X] T052 [P] [US1] Create SignInPage.tsx (FluentProvider wrapping a centered layout with a Fluent UI Button "Sign in with Google" linking to /api/v1/auth/signin) in src/Kanban.Web/src/pages/SignInPage.tsx — **GREEN commit for T043**
- [X] T053 [P] [US1] Create NotRegisteredPage.tsx (FluentProvider + MessageBar or Text: "You are not registered to use this application. Please contact the administrator.") in src/Kanban.Web/src/pages/NotRegisteredPage.tsx — **GREEN commit for T044**
- [X] T054 [US1] Implement useCurrentUser hook (useQuery ['currentUser'] calling GET /api/v1/auth/me; handles 403 → isNotRegistered flag; handles 401 → isUnauthenticated flag) in src/Kanban.Web/src/hooks/useCurrentUser.ts
- [X] T055 [US1] Set up React Router in src/Kanban.Web/src/App.tsx: routes for / (placeholder landing page), /accept/:token (AcceptInvitePage placeholder), /not-registered (NotRegisteredPage), /signin (SignInPage); root route guards: if !authenticated → /signin, if notRegistered → /not-registered

**Checkpoint**: US1 complete. Admin signs in, lands on landing page. Unregistered Google user → 403 "not registered". All tests for US1 green: `dotnet test tests/unit/`, `dotnet test tests/integration/`, `npm test` (SignInPage, NotRegisteredPage), `npx playwright test tests/e2e/Auth/AdminSignInTests.cs`.

---

## Phase 4: User Story 2 — Admin Issues An Invitation (Priority: P1)

**Goal**: Signed-in admin can enter an email, receive a redemption link in the UI, and copy it
to share out-of-band. Re-inviting an email that has an active invite returns the existing link.
Expired invites can be re-issued.

**Independent Test** (from spec.md US2): Sign in as admin, enter an invitee email, submit,
confirm a redemption link is returned containing a token. Confirm the invitation is recorded
with a future expiry. Confirm re-invite of same email returns the same link.

### Tests for US2 — Write FIRST, ensure they FAIL ⚠️

- [X] T056 [P] [US2] Write failing unit tests for InvitationService.IssueAsync: (1) new email → invitation created, token generated, InvitationIssued AuthEvent logged; (2) active invite exists for email → same token hash returned, no new record; (3) expired invite exists → fresh invite issued; (4) email already registered → ConflictException code:invite.already_registered; (5) non-admin caller → ForbiddenException in tests/unit/Business/InvitationServiceTests.cs — **RED commit**
- [X] T057 [P] [US2] Write failing integration tests for POST /api/v1/invites: 201 new invite (token in response, expiresAt 7 days out); 200 re-invite of active email (same token); 409 already-registered email; 422 invalid email format; 403 non-admin caller in tests/integration/Api/InviteEndpointTests.cs — **RED commit**
- [X] T058 [P] [US2] Write failing RTL tests for InviteUserDialog: renders email TextField with label; submit button disabled when email empty; successful mutation renders redemption link in copyable field; 409 renders "already registered" inline error; 422 renders email validation error in src/Kanban.Web/tests/components/InviteUserDialog.test.tsx — **RED commit**
- [X] T059 [P] [US2] Write failing Playwright e2e tests for US2 acceptance scenarios 1–5 in tests/e2e/Auth/InviteAcceptanceTests.cs — **RED commit**

### Implementation for US2

- [X] T060 [US2] Add UserTransforms.ToDto and InvitationTransforms.ToResponse mapping methods (entity → DTO) in src/Kanban.Business/Transforms/UserTransforms.cs and src/Kanban.Business/Transforms/InvitationTransforms.cs
- [X] T061 [US2] Implement IssueInviteRequestValidator (FluentValidation: NotEmpty + EmailAddress) in src/Kanban.Api/Validators/IssueInviteRequestValidator.cs; register AddFluentValidationAutoValidation in Program.cs
- [X] T062 [US2] Implement IInvitationService.IssueAsync in InvitationService (Verify.That all params; FindActiveByEmail → return existing; FindByEmail for registered user → throw ConflictException; InvitationToken.Generate; Insert in deferred txn; log AuthEvent InvitationIssued; return IssueInviteResponse with raw token + redemption link) in src/Kanban.Business/Services/InvitationService.cs — **GREEN commit for T056**
- [X] T063 [US2] Implement POST /api/v1/invites endpoint (requires Admin policy; validates request via FluentValidation; calls IInvitationService.IssueAsync; returns 201 for new invite, 200 for existing) in src/Kanban.Api/Endpoints/InviteEndpoints.cs — **GREEN commit for T057**
- [X] T064 [US2] Implement useIssueInvite hook (useMutation posting to /api/v1/invites; onError: persistent Fluent UI toast "Could not issue invitation. Try again."; exposes { mutate, data, error, isPending }) in src/Kanban.Web/src/hooks/useIssueInvite.ts
- [X] T065 [US2] Create InviteUserDialog.tsx (Fluent UI Dialog with TextField for email + primary Button "Send Invitation"; on success show redemption link in Input with CopyButton; on error show inline MessageBar) in src/Kanban.Web/src/components/admin/InviteUserDialog.tsx — **GREEN commit for T058**

**Checkpoint**: US2 complete. Admin can issue invitations and receive redemption links. All US2 tests green.

---

## Phase 5: User Story 3 — Invitee Accepts An Invitation (Priority: P1)

**Goal**: Invitee opens the redemption link, completes Google sign-in with the matching email,
is registered as a user, and can sign in again on future visits without the link.

**Independent Test** (from spec.md US3): Issue invitation (US2), open link, complete Google
sign-in with matching email, confirm invitee lands on signed-in landing page. Sign out, sign
back in via regular flow, confirm recognized as same registered user.

### Tests for US3 — Write FIRST, ensure they FAIL ⚠️

- [ ] T066 [P] [US3] Write failing unit tests for InvitationService.AcceptAsync: (1) valid token + matching email → user created, invite consumed, UserRegistered AuthEvent logged; (2) expired token → NotFoundException code:invite.invalid; (3) already-consumed token → NotFoundException code:invite.invalid; (4) non-existent token → NotFoundException code:invite.invalid; (5) email mismatch → BusinessRuleException code:invite.email_mismatch; (6) concurrent accept on same token — TryConsumeAsync returns false on second call → NotFoundException in tests/unit/Business/InvitationServiceTests.cs (extend existing file) — **RED commit**
- [ ] T067 [P] [US3] Write failing integration tests for POST /api/v1/invites/{token}/accept: 200 success + CurrentUserDto + auth cookie now contains user_id/system_role; 410 expired token; 410 consumed token; 410 invalid/non-existent token (identical 410 for all three); 422 email mismatch; concurrent test: 20 parallel requests for same valid token → exactly 1 returns 200, all others return 410 in tests/integration/Api/InviteEndpointTests.cs (extend existing file) — **RED commit**
- [ ] T068 [P] [US3] Write failing RTL tests for AcceptInvitePage: unauthenticated state renders "Accept & Sign in with Google" button; authenticated state auto-calls accept; on 200 displays "Welcome" and redirects; on 410 displays "invitation is no longer valid" error; on 422 displays "issued to a different email" error in src/Kanban.Web/tests/components/AcceptInvitePage.test.tsx — **RED commit**
- [ ] T069 [P] [US3] Add failing Playwright e2e tests for US3 acceptance scenarios 1–6 to tests/e2e/Auth/InviteAcceptanceTests.cs: (1) valid accept → signed-in page; (2) subsequent sign-in via regular flow → recognized; (3) expired token → refusal message; (4) consumed token → same refusal message; (5) email mismatch → "different email" message; (6) fabricated token → same refusal message as expired/consumed — **RED commit**

### Implementation for US3

- [ ] T070 [US3] Implement IInvitationService.AcceptAsync in InvitationService: hash raw token; begin deferred txn; TryConsumeAsync — if false → throw NotFoundException (uniform message, no specifics); find invitation for token hash; call invitation.EmailMatches → if false → throw BusinessRuleException; insert User record; commit; log InvitationAccepted AuthEvent; return created User entity; wrap in Polly retry on SqliteException SQLITE_BUSY in src/Kanban.Business/Services/InvitationService.cs — **GREEN commit for T066**
- [ ] T071 [US3] Implement POST /api/v1/invites/{token}/accept endpoint: RequireAuthorization (not RegisteredUser); call IInvitationService.AcceptAsync(rawToken, googleEmail from claims); on success — add user_id and system_role claims to current identity and call HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, updatedPrincipal) to refresh cookie; return 200 CurrentUserDto in src/Kanban.Api/Endpoints/InviteEndpoints.cs — **GREEN commit for T067**
- [ ] T072 [US3] Implement useAcceptInvite hook (useMutation posting to /api/v1/invites/:token/accept; onError: persistent toast; exposes { mutate, data, error }) in src/Kanban.Web/src/hooks/useAcceptInvite.ts
- [ ] T073 [US3] Create AcceptInvitePage.tsx: reads :token from route params; if not authenticated → renders "Accept & Sign in with Google" Button (href → /api/v1/auth/signin?returnUrl=/accept/:token); if authenticated (cookie present) and token in URL → auto-calls useAcceptInvite.mutate on mount; on 200 → navigate to /; on 410 → renders "This invitation is no longer valid" MessageBar; on 422 → renders "This invitation was issued to a different email" MessageBar in src/Kanban.Web/src/pages/AcceptInvitePage.tsx — **GREEN commit for T068**

**Checkpoint**: US3 complete. Full onboarding loop works end-to-end. All US3 tests green including concurrent acceptance test.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verify audit integrity, information-leakage gates, and spec success criteria.

- [ ] T074 [P] Verify FR-021 audit integrity: add assertions to existing unit tests confirming AuthEvent records for SignIn, InvitationIssued, InvitationAccepted, AcceptanceRefused contain no email addresses, token values, or display names — only GUIDs, event types, and outcome codes in tests/unit/Business/AuthServiceTests.cs and InvitationServiceTests.cs
- [ ] T075 [P] Verify SC-005 information-leakage gate: assert that the response body for expired token, consumed token, and fabricated token are byte-for-byte identical in tests/integration/Api/InviteEndpointTests.cs
- [ ] T076 [P] Verify SC-007 concurrent acceptance: confirm the 20-parallel-request integration test in InviteEndpointTests.cs shows exactly 1 success (200) and 19 failures (410) with zero duplicate user records in DB
- [ ] T077 Run quickstart.md validation: follow all steps from specs/001-auth-onboarding/quickstart.md on a clean checkout; update quickstart.md if any step is incorrect or missing
- [ ] T078 [P] Run Snyk SCA + SAST scan on all backend projects and Kanban.Web; resolve any medium/high/critical findings before marking feature complete
- [ ] T079 [P] Verify gitleaks scan on branch diff: `gitleaks detect --source .` confirms no secrets in any committed file
- [ ] T080 Run all four test layers and confirm ≥ 90% coverage: `dotnet test tests/unit/ tests/integration/`; `cd src/Kanban.Web && npm test`; `npx playwright test tests/e2e/Auth/`
- [ ] T081 Update SESSION_LOG.md with this implementation session

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundation)**: Depends on Phase 1 completion — **BLOCKS all user stories**
- **Phase 3 (US1)**: Depends on Phase 2 completion
- **Phase 4 (US2)**: Depends on Phase 3 completion (admin must be able to sign in before invites are useful)
- **Phase 5 (US3)**: Depends on Phase 4 completion (invitation must exist to accept)
- **Phase 6 (Polish)**: Depends on all story phases complete

### Within Phase 2 (Foundation Internal Order)

1. T012–T013: Verify.cs (all other domain code depends on it)
2. T014–T023: Exception hierarchy + domain entities + enums (can run in parallel once T013 done)
3. T024–T026: DB migrations
4. T027: Repository interfaces (depends on domain entities)
5. T028–T033: Repository implementations + integration tests (depends on T027, T030 fixture)
6. T034–T037: API infrastructure (depends on T015 exceptions)
7. T038–T040: Builders + frontend setup (parallelizable)

### Within Each Story Phase (Internal Order)

1. All test tasks [P] first — write and commit as RED
2. Domain/service implementations next (unblocked in parallel once tests written)
3. API endpoint implementations (depend on service implementations)
4. Frontend hooks (parallelizable with API)
5. Frontend components (depend on hooks)
6. Checkpoint verification last

### Parallel Opportunities Per Phase

**Phase 2**: T014+T016+T017+T018+T019 can all start simultaneously after T013. T028+T029 can be written in parallel.

**Phase 3 (US1)**: T041+T042+T043+T044+T045 all written in parallel (RED). Then T046+T052+T053 in parallel. Then T048+T049+T054 in parallel. T047 depends on T046. T050 depends on T047+T048+T049. T051 depends on T050.

**Phase 4 (US2)**: T056+T057+T058+T059 written in parallel (RED). T060+T061+T064 in parallel. T062 depends on T060.

**Phase 5 (US3)**: T066+T067+T068+T069 written in parallel (RED). T072+T073 can start after T070+T071.

---

## Parallel Example: Phase 3 (US1)

```
# Round 1 — write all failing tests simultaneously (RED commits):
T041: Write AuthServiceTests.cs
T042: Write AuthEndpointTests.cs
T043: Write SignInPage.test.tsx
T044: Write NotRegisteredPage.test.tsx
T045: Write AdminSignInTests.cs (Playwright)

# Round 2 — domain/infrastructure (unblocked):
T046: Implement GoogleIdentityAdapter
T052: Create SignInPage.tsx
T053: Create NotRegisteredPage.tsx

# Round 3 — business + auth handlers (unblocked after T046):
T047: Implement AuthService          ← GREEN for T041
T048: Implement CurrentUserService
T049: Implement RegisteredUserRequirement/Handler

# Round 4 — API wiring (depends on T047+T048+T049):
T050: Wire Google OIDC in Program.cs
T054: Implement useCurrentUser hook

# Round 5 — endpoints + routing:
T051: Implement AuthEndpoints        ← GREEN for T042
T055: Set up React Router + App.tsx  ← GREEN for T043+T044
```

---

## Implementation Strategy

### MVP Increment (US1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundation
3. Complete Phase 3: User Story 1 — admin sign-in + gate
4. **STOP and VALIDATE**: admin can sign in; unregistered users are refused
5. All 4 test layers pass for US1

### Full Feature (All Three P1 Stories)

1. Setup + Foundation → app boots, DB migrated, health checks respond
2. US1 → admin signs in, gate refuses strangers (**MVP checkpoint**)
3. US2 → admin issues invitations
4. US3 → invitee accepts, full onboarding loop closes
5. Polish → audit integrity, leakage gates, Snyk/gitleaks clean

---

## Notes

- `[P]` tasks = different files, no unresolved dependencies — safe to run simultaneously
- `[US1]`/`[US2]`/`[US3]` maps each task to the spec user story for traceability
- Every RED commit must precede its GREEN commit in git history (TDD evidence)
- Commit after each task or small logical group — do not batch across RED/GREEN boundary
- Stop at any checkpoint to validate the story independently before proceeding
- `Verify.That` calls are required in all non-API public method bodies (constitution)
- No `TransactionScope` — `IDbTransaction` only; pass transaction explicitly to all Dapper calls
