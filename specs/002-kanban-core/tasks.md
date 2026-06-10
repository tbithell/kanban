# Tasks: Kanban Core — Board, Lane, Card, and Membership Management

**Branch**: `002-kanban-core` | **Input**: `specs/002-kanban-core/plan.md` + `spec.md`

**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

**Tests**: TDD is mandatory (constitution Principle II). Test tasks are written **first** and
**must fail** before implementation begins. A failing-test commit MUST precede each
passing-implementation commit. All four test layers are required:
xUnit (unit + integration), RTL + Vitest (component), Playwright (e2e).

**Organization**: Tasks are grouped by user story. Foundational work in Phase 2 MUST be
complete before any user story phase begins.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no conflicting dependencies)
- **[Story]**: Which user story this task belongs to (US1–US5 from spec.md)
- Exact file paths included in every description

---

## Phase 0: Validation Unification Prerequisite (US6)

**Purpose**: Replace the bespoke `Verify` guard-clause class with FluentValidation across all
non-API layers. MUST complete before any US1–US5 service work — all new services in Phases 1–8
adopt FluentValidation from day one. TDD order: update tests to assert `ValidationException`
(RED commit) → update source to use FluentValidation (GREEN commit) → delete `Verify.cs`.

**Scope**: 146 `Verify.` call sites across 20 source files; 36 test assertions in 4 test files.

**Setup**

- [X] T088 Add `FluentValidation` NuGet package reference to `src/Kanban.Domain/Kanban.Domain.csproj`, `src/Kanban.Business/Kanban.Business.csproj`, `src/Kanban.DataAccess/Kanban.DataAccess.csproj`, and `src/Kanban.AntiCorruption/Kanban.AntiCorruption.csproj` — match the exact version already in `src/Kanban.Api/Kanban.Api.csproj`; run `dotnet restore` to confirm

**Tests — RED commits first (must fail before implementation)**

- [X] T089 [US6] Update `tests/unit/Domain/UserTests.cs` (8 assertions), `tests/unit/Domain/InvitationTests.cs` (2), and `tests/unit/Domain/InvitationTokenTests.cs` (2) — change every `ArgumentNullException`/`ArgumentException` assertion to `FluentValidation.ValidationException`; add `using FluentValidation` where needed; COMMIT RED (tests fail because entities still throw via `Verify`)
- [X] T090 [US6] Delete `tests/unit/Domain/VerifyTests.cs` — this file tests the `Verify` class itself; when `Verify.cs` is deleted the test has no subject; behavior coverage is now carried by the entity tests updated in T089; COMMIT alongside T089 red commit
- [X] T091 [US6] Add integration test `tests/integration/Api/ValidationExceptionMappingTests.cs` — POST to an endpoint that reaches a service method, stub or arrange an invalid argument to trigger a `ValidationException` from the Business layer, assert `422` HTTP status and Problem Details body with `"code": "validation.failed"`; COMMIT RED (handler not yet wired)

**Implementation — GREEN commits**

- [X] T092 [US6] Update `src/Kanban.Api/ExceptionHandlers/DomainExceptionHandler.cs` — add a `FluentValidation.ValidationException` catch case before the catch-all; map to `422 Unprocessable Entity`; set `code: "validation.failed"` and populate an `errors` array from `exception.Errors` (field name + message per entry); COMMIT GREEN for T091
- [X] T093 [P] [US6] Replace `Verify.That` calls in `src/Kanban.Domain/Entities/Board.cs` (4 calls), `src/Kanban.Domain/Entities/Lane.cs` (6), `src/Kanban.Domain/Entities/Card.cs` (10), `src/Kanban.Domain/Entities/BoardMember.cs` (3) — use `new InlineValidator<T> { v => v.RuleFor(...).NotEmpty() }.ValidateAndThrow(value)` for string/Guid/int params; use `ArgumentNullException.ThrowIfNull()` for injected service references (programming-error guard, not user-input)
- [X] T094 [P] [US6] Replace `Verify.That` calls in `src/Kanban.Domain/Entities/User.cs` (4), `src/Kanban.Domain/Entities/Invitation.cs` (6), `src/Kanban.Domain/Entities/CardAssignee.cs` (3), `src/Kanban.Domain/ValueObjects/InvitationToken.cs` (1) — same inline validator pattern
- [X] T095 [P] [US6] Replace `Verify.That` calls in `src/Kanban.Business/Services/InvitationService.cs` (14) and `src/Kanban.Business/Services/AuthService.cs` (9) — for each public method, create or inline a `AbstractValidator<(param1, param2, ...)>` (or a named record + validator) and call `.ValidateAndThrow()`; create `src/Kanban.Business/Validators/` directory for named validator classes
- [X] T096 [P] [US6] Replace `Verify.That` calls in `src/Kanban.Business/Transforms/InvitationTransforms.cs` (5) and `src/Kanban.Business/Transforms/UserTransforms.cs` (1) — use `InlineValidator<T>`
- [X] T097 [P] [US6] Replace `Verify.That` calls in `src/Kanban.DataAccess/Repositories/LaneRepository.cs` (15), `src/Kanban.DataAccess/Repositories/CardRepository.cs` (15), `src/Kanban.DataAccess/Repositories/InvitationRepository.cs` (13), `src/Kanban.DataAccess/Repositories/BoardMemberRepository.cs` (13) — for primitive params (`string`, `Guid`) use `InlineValidator<string>` / `InlineValidator<Guid>`; for IDbConnection / IDbTransaction params use `ArgumentNullException.ThrowIfNull()`
- [X] T098 [P] [US6] Replace `Verify.That` calls in `src/Kanban.DataAccess/Repositories/UserRepository.cs` (11), `src/Kanban.DataAccess/Repositories/BoardRepository.cs` (9), `src/Kanban.DataAccess/Repositories/AuthEventRepository.cs` (3), and `src/Kanban.AntiCorruption/Adapters/GoogleIdentityAdapter.cs` (1) — same inline validator pattern
- [X] T099 [US6] Delete `src/Kanban.Domain/Verify.cs` — all 146 usages replaced; run `dotnet build` from repo root and confirm zero errors and zero warnings; fix any stray references before committing
- [X] T100 [US6] Run `dotnet test tests/unit/ tests/integration/`; confirm all tests green; confirm zero references to `ParameterVerifier<T>`, `StringVerifierExtensions`, `NumberVerifierExtensions`, `ComparableVerifierExtensions`, `EnumerableVerifierExtensions` in compiled output via `grep -rn "ParameterVerifier\|Verify\." src/`; COMMIT GREEN

**Checkpoint**: `dotnet build` 0 errors / 0 warnings; all unit and integration tests pass; no `Verify.` references remain anywhere in `src/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Database schema, domain entities, contracts, and test builders — the raw material
every user story depends on. No business logic here; these are data structures only.

- [X] T001 Install dnd-kit packages: run `npm install @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities` in `src/Kanban.Web/`
- [X] T002 [P] Create SQLite migration 003 — boards, board_members, lanes, cards, card_assignees tables with indexes and cascade rules — in `src/Kanban.Data/migrations/sqlite/003_boards_lanes_cards.sql` (use schema from data-model.md exactly)
- [X] T003 [P] Create Postgres migration 003 (identical SQL — all syntax is ANSI-compatible) in `src/Kanban.Data/migrations/postgres/003_boards_lanes_cards.sql`
- [X] T004 [P] Create SQLite migration 004 — add nullable `board_id` and `board_role` columns to `invitations` — in `src/Kanban.Data/migrations/sqlite/004_extend_invitations.sql`
- [X] T005 [P] Create Postgres migration 004 (identical SQL) in `src/Kanban.Data/migrations/postgres/004_extend_invitations.sql`
- [X] T006 [P] Create `BoardRole` enum (`Owner`, `Member`, `Viewer`) in `src/Kanban.Domain/Enums/BoardRole.cs`
- [X] T007 [P] Create `Board` aggregate root entity with `Verify.That` guards and `Rename(string)` method in `src/Kanban.Domain/Entities/Board.cs` (see data-model.md for signature)
- [X] T008 [P] Create `Lane` aggregate root entity with `Verify.That` guards, `Rename(string)`, and `MoveTo(int)` (increments `Version`) in `src/Kanban.Domain/Entities/Lane.cs`
- [X] T009 [P] Create `Card` aggregate root entity with `Verify.That` guards, `Update(...)`, and `MoveTo(Guid, int, DateTimeOffset)` (increments `Version`) in `src/Kanban.Domain/Entities/Card.cs`
- [X] T010 [P] Create `BoardMember` entity with `Verify.That` guards and `ChangeRole(BoardRole)` in `src/Kanban.Domain/Entities/BoardMember.cs`
- [X] T011 [P] Create `CardAssignee` entity (data model only — no service in this feature) in `src/Kanban.Domain/Entities/CardAssignee.cs`
- [X] T012 [P] Extend `Invitation` entity with `public Guid? BoardId { get; init; }` and `public BoardRole? BoardRole { get; init; }` in `src/Kanban.Domain/Entities/Invitation.cs`
- [X] T013 [P] Create all response DTOs in `src/Kanban.Contracts/`: `BoardRoleDto.cs` (enum), `BoardSummaryDto.cs`, `BoardDetailDto.cs`, `LaneDto.cs`, `CardDto.cs`, `BoardMemberDto.cs`, `AcceptInviteResponseDto.cs` (wraps `CurrentUserDto` + `Guid? BoardId`) — use shapes from contracts/dtos.md
- [X] T014 [P] Create all request DTOs in `src/Kanban.Contracts/`: `CreateBoardRequest.cs`, `CreateLaneRequest.cs`, `RenameLaneRequest.cs`, `MoveLaneRequest.cs`, `CreateCardRequest.cs`, `UpdateCardRequest.cs`, `MoveCardRequest.cs`, `InviteBoardMemberRequest.cs`, `ChangeMemberRoleRequest.cs` — use shapes from contracts/dtos.md
- [X] T015 [P] Create test data builders in `tests/unit/Builders/`: `BoardBuilder.cs`, `LaneBuilder.cs`, `CardBuilder.cs`, `BoardMemberBuilder.cs` — each with a static factory method (e.g., `ABoard()`), fluent setters, and `.Build()` returning a valid entity with sensible defaults (see constitution Test Data Builders section)

**Checkpoint**: Domain layer and contracts compile; migrations present; builders ready. No business logic yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: DataAccess repositories, business service interfaces, transforms, API auth, validators,
and DI registration. MUST be complete before any user story work begins.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T016 [P] Create `IBoardRepository` interface (`FindBoardForMemberAsync`, `FindBoardsForUserAsync`, `CreateAsync`, `ExistsWithNameAsync`, `DeleteAsync`) in `src/Kanban.DataAccess/Interfaces/IBoardRepository.cs`
- [X] T017 [P] Create `ILaneRepository` interface (`FindByBoardAsync`, `FindByIdAsync`, `CreateAsync`, `UpdateNameAsync`, `UpdatePositionAsync`, `ShiftPositionsAsync`, `DeleteAsync`, `CountInBoardAsync`) in `src/Kanban.DataAccess/Interfaces/ILaneRepository.cs`
- [X] T018 [P] Create `ICardRepository` interface (`FindByLaneAsync`, `FindByIdAsync`, `CreateAsync`, `UpdateAsync`, `UpdatePositionAsync`, `ShiftPositionsInLaneAsync`, `DeleteAsync`, `CountInLaneAsync`) in `src/Kanban.DataAccess/Interfaces/ICardRepository.cs`
- [X] T019 [P] Create `IBoardMemberRepository` interface (`FindRoleAsync`, `CountOwnersAsync`, `AddAsync`, `RemoveAsync`, `UpdateRoleAsync`, `FindAllForBoardAsync`) in `src/Kanban.DataAccess/Interfaces/IBoardMemberRepository.cs`
- [X] T020 [P] Implement `BoardRepository` using Dapper + `IDbConnection` — `FindBoardForMemberAsync` returns null for non-members (enumeration prevention); board+lanes+cards loaded via single LEFT JOIN query in `src/Kanban.DataAccess/Repositories/BoardRepository.cs`
- [X] T021 [P] Implement `LaneRepository` using Dapper — includes position-shift batch UPDATE queries inside passed `IDbTransaction`; insert + separate SELECT (no RETURNING) in `src/Kanban.DataAccess/Repositories/LaneRepository.cs`
- [X] T022 [P] Implement `CardRepository` using Dapper — includes cross-lane position-shift queries; `board_id` written on insert (denormalized); insert + separate SELECT in `src/Kanban.DataAccess/Repositories/CardRepository.cs`
- [X] T023 [P] Implement `BoardMemberRepository` using Dapper — `CountOwnersAsync` used by last-owner guard in `src/Kanban.DataAccess/Repositories/BoardMemberRepository.cs`
- [X] T024 [P] Create business service interfaces in `src/Kanban.Business/Interfaces/`: `IBoardService.cs`, `ILaneService.cs`, `ICardService.cs`, `IBoardMembershipService.cs` — method signatures match plan.md Project Structure comments
- [X] T025 [P] Create static transform classes in `src/Kanban.Business/Transforms/`: `BoardTransforms.cs` (`Board → BoardSummaryDto`, `Board + Lanes → BoardDetailDto`), `LaneTransforms.cs` (`Lane + Cards → LaneDto`), `CardTransforms.cs` (`Card → CardDto`), `BoardMemberTransforms.cs` (`BoardMember + User → BoardMemberDto`)
- [X] T026 [P] Create FluentValidation validators for all nine new request DTOs in `src/Kanban.Api/Validators/` — rules from contracts/dtos.md validation table; register validators via `AddFluentValidationAutoValidation()` (already wired in Program.cs pattern from 001)
- [X] T027 [P] Create `BoardOperations` static class with operation-constant strings (`Read`, `CreateCard`, `UpdateCard`, `DeleteCard`, `CreateLane`, `UpdateLane`, `DeleteLane`, `ManageMembers`, `DeleteBoard`) in `src/Kanban.Api/Auth/BoardOperations.cs`
- [X] T028 [P] Create `BoardMembershipRequirement : IAuthorizationRequirement` and `BoardContext` value type (`{ Guid BoardId, BoardRole ResolvedRole }`) in `src/Kanban.Api/Auth/BoardMembershipRequirement.cs`
- [X] T029 Implement `BoardAuthorizationHandler : AuthorizationHandler<BoardMembershipRequirement, BoardContext>` — injects `IBoardMemberRepository`; resolves caller's board role from DB; maps `BoardOperations` constants to minimum required role in `src/Kanban.Api/Auth/BoardAuthorizationHandler.cs`
- [X] T030 Register all new repositories (`BoardRepository`, `LaneRepository`, `CardRepository`, `BoardMemberRepository`), service interfaces (stubs — actual implementations registered in later phases), validators, and `BoardAuthorizationHandler` as scoped in `src/Kanban.Api/Program.cs`; add rate-limiter policy overrides (`anonymous`, `authenticated`, `mutating` at 10,000 permits) to `tests/integration/KanbanWebAppFactory.cs` for the three new policies

**Checkpoint**: Solution compiles; all DI registrations resolve; rate-limiter policies present in test factory.

---

## Phase 3: User Story 1 — Admin Creates a Board and Adds Lanes (Priority: P1) 🎯 MVP

**Goal**: Admin can create a named board (auto-assigned as Owner), add ordered lanes, and retrieve
the board with its lanes in order. Non-admins cannot create boards; non-members get 404.

**Independent Test**: Sign in as admin → create board → add three lanes → retrieve board → verify
lane order and owner assignment. Attempt board creation as non-admin → verify 403. Attempt board
access as non-member → verify 404.

### Tests for US1 (write FIRST — must fail before implementation)

- [X] T031 [P] [US1] Write failing unit tests (8 tests) for `BoardService` — create success, name conflict 409, non-admin 403, list boards for member, get board detail, get board as non-member returns NotFoundException, delete as Owner, delete as non-Owner 403 — in `tests/unit/Business/BoardServiceTests.cs`; use `BoardBuilder`, mock `IBoardRepository` and `IAuthorizationService`
- [X] T032 [P] [US1] Write failing unit tests (10 tests) for `LaneService` — create success (position = N+1), name conflict 409, rename success, rename to duplicate name 409, delete with cascade, move lane (positions shift), position gapless after delete, position gapless after reorder, viewer cannot create 403, create on non-existent board 404 — in `tests/unit/Business/LaneServiceTests.cs`; use `LaneBuilder`, mock repos
- [X] T033 [P] [US1] Write failing integration tests for `BoardEndpoints` — `GET /api/v1/boards` returns member boards, `POST /api/v1/boards` (admin creates, non-admin 403, duplicate name 409, validation 422), `GET /api/v1/boards/{id}` (member 200, non-member 404), `DELETE /api/v1/boards/{id}` (owner 204, non-owner 403, non-member 404) — in `tests/integration/Api/BoardEndpointTests.cs`
- [X] T034 [P] [US1] Write failing integration tests for `LaneEndpoints` — `POST /lanes` (success 201, viewer 403, name conflict 409), `PATCH /lanes/{id}` (rename success, duplicate 409), `DELETE /lanes/{id}` (success 204, viewer 403), `POST /lanes/{id}/move` (success 200, version mismatch 409, viewer 403) — in `tests/integration/Api/LaneEndpointTests.cs`
- [X] T035 [P] [US1] Write failing Playwright e2e tests for US1 acceptance scenarios 1–6 in `tests/e2e/BoardManagementTests.cs` — create board (admin), add three lanes, verify order, validation rejection, non-admin rejection, duplicate name rejection; use bypass-auth dev endpoint for admin persona

### Implementation for US1

- [X] T036 [US1] Implement `BoardService` (create with Owner board_member insert, list for user, get via FindBoardForMemberAsync, delete; enforce Admin policy on create, Owner/Admin on delete; use Polly deferred transaction) in `src/Kanban.Business/Services/BoardService.cs`; register in `Program.cs`
- [X] T037 [US1] Implement `LaneService` (create appended at position N+1, rename with uniqueness check, delete, reorder with batch position shift inside deferred transaction + Polly; enforce Owner/Member role; version check on reorder → 409 on mismatch) in `src/Kanban.Business/Services/LaneService.cs`; register in `Program.cs`
- [X] T038 [P] [US1] Implement `BoardEndpoints` (GET/POST boards, GET/DELETE board by ID) in `src/Kanban.Api/Endpoints/BoardEndpoints.cs`; map to versioned `/api/v1` group with `RequireRateLimiting` and `RequireAuthorization`; register endpoint group in `Program.cs`
- [X] T039 [P] [US1] Implement `LaneEndpoints` (POST/PATCH/DELETE lane, POST move) in `src/Kanban.Api/Endpoints/LaneEndpoints.cs`; register in `Program.cs`
- [X] T040 [P] [US1] Write RTL component tests for `BoardListPage` — renders board list, shows board name and role, admin sees "Create Board" button, non-admin does not, empty state when no boards — in `src/Kanban.Web/tests/components/BoardListPage.test.tsx`
- [X] T041 [P] [US1] Write RTL component tests for `BoardPage` — renders board title and ordered lane list, shows "Add Lane" form, heading level 1 present (WCAG AA gate) — in `src/Kanban.Web/tests/components/BoardPage.test.tsx`
- [X] T042 [P] [US1] Implement `useBoards` (`['boards']` query) and `useBoard(boardId)` (`['boards', boardId]` query) hooks in `src/Kanban.Web/src/hooks/useBoards.ts` and `useBoard.ts`
- [X] T043 [P] [US1] Implement `useCreateBoard` and `useDeleteBoard` mutation hooks with `onSettled` invalidation in `src/Kanban.Web/src/hooks/useCreateBoard.ts` and `useDeleteBoard.ts`
- [X] T044 [P] [US1] Implement `useCreateLane`, `useRenameLane`, `useDeleteLane` mutation hooks with `onSettled` invalidation in `src/Kanban.Web/src/hooks/useCreateLane.ts`, `useRenameLane.ts`, `useDeleteLane.ts`
- [X] T045 [P] [US1] Implement `BoardCard` (board summary card with name, lane count, card count, role badge) and `CreateBoardDialog` (Fluent UI Dialog, name field, submit/cancel) in `src/Kanban.Web/src/components/boards/BoardCard.tsx` and `CreateBoardDialog.tsx`
- [X] T046 [US1] Implement `BoardListPage` — fetches `useBoards`, renders `BoardCard` list, shows `CreateBoardDialog` for admin only, navigates to board on click — in `src/Kanban.Web/src/pages/BoardListPage.tsx`; add route in app router
- [X] T047 [US1] Implement `BoardPage` (skeleton: renders board title, lane list with `AddLaneForm`; no cards or DnD yet) and `Lane` component (non-sortable: shows lane name, empty card placeholder, delete/rename controls for Owner/Member) and `AddLaneForm` in `src/Kanban.Web/src/pages/BoardPage.tsx`, `src/Kanban.Web/src/components/board/Lane.tsx`, `src/Kanban.Web/src/components/board/AddLaneForm.tsx`

**Checkpoint**: Admin can create boards and add lanes via UI; all US1 unit, integration, and e2e tests green.

---

## Phase 4: User Story 2 — Board Member Adds and Manages Cards (Priority: P1)

**Goal**: Board owners and members can create cards (title required, description and due date optional),
update all card fields, clear the due date explicitly, and delete cards. Viewers cannot write.
Positions remain gapless after every operation.

**Independent Test**: Member adds card → retrieves it → updates title → clears due date → deletes →
confirms gapless positions on remaining cards. Viewer create/update/delete attempts → all 403.

### Tests for US2 (write FIRST — must fail before implementation)

- [ ] T048 [P] [US2] Write failing unit tests (12 tests) for `CardService` — create success (appended at N+1), create with description + dueDate, update title only, update all fields, clear dueDate via `ClearDueDate=true`, delete (positions shift), move same lane (positions within lane adjust), move cross-lane (both lanes adjust), version conflict → ConflictException 409, position gapless after delete, position gapless after same-lane move, viewer 403 — in `tests/unit/Business/CardServiceTests.cs`; use `CardBuilder` and `LaneBuilder`
- [ ] T049 [P] [US2] Write failing integration tests for `CardEndpoints` — `POST /lanes/{laneId}/cards` (201, viewer 403, validation 422), `PATCH /cards/{cardId}` (200 updated, 200 cleared dueDate), `DELETE /cards/{cardId}` (204, remaining positions gapless), `POST /cards/{cardId}/move` (200 same lane, 200 cross lane, 409 concurrent version conflict with 3–5 concurrent requests) — in `tests/integration/Api/CardEndpointTests.cs`
- [ ] T050 [P] [US2] Write failing Playwright e2e tests for US2 acceptance scenarios 1–6 in `tests/e2e/CardManagementTests.cs` — add card, update all fields, clear due date, delete with gapless positions, viewer forbidden, title validation
- [ ] T051 [P] [US2] Write RTL component tests for `CardItem` — renders card title, shows description indicator when present, edit button triggers dialog, keyboard accessible — in `src/Kanban.Web/tests/components/CardItem.test.tsx`

### Implementation for US2

- [X] T052 [US2] Implement `CardService` (create appended at N+1, update with `ClearDueDate` handling, delete with position shift, move same-lane and cross-lane both inside deferred transaction + Polly; version check on move → 409; enforce Owner/Member role; Verify.That on all public params) in `src/Kanban.Business/Services/CardService.cs`; register in `Program.cs`
- [X] T053 [US2] Implement `CardEndpoints` (`POST /boards/{boardId}/lanes/{laneId}/cards`, `PATCH /boards/{boardId}/cards/{cardId}`, `DELETE /boards/{boardId}/cards/{cardId}`, `POST /boards/{boardId}/cards/{cardId}/move`) in `src/Kanban.Api/Endpoints/CardEndpoints.cs`; register in `Program.cs`
- [X] T054 [P] [US2] Implement `useCreateCard`, `useUpdateCard`, `useDeleteCard` mutation hooks with `onSettled` cache invalidation in `src/Kanban.Web/src/hooks/useCreateCard.ts`, `useUpdateCard.ts`, `useDeleteCard.ts`
- [X] T055 [P] [US2] Implement `CardItem` component (displays title, due-date chip, description indicator; opens `CardDetailDialog` on click; delete button for Owner/Member) in `src/Kanban.Web/src/components/board/CardItem.tsx`
- [X] T056 [P] [US2] Implement `AddCardForm` (inline form: title input, submit, cancel; appended at bottom of lane) in `src/Kanban.Web/src/components/board/AddCardForm.tsx`
- [X] T057 [US2] Implement `CardDetailDialog` (Fluent UI Dialog: edit title, description textarea, date picker, clear-due-date checkbox, save/delete/cancel) in `src/Kanban.Web/src/components/board/CardDetailDialog.tsx`; use `useUpdateCard` and `useDeleteCard`
- [X] T058 [US2] Update `Lane` component to render `CardItem` list and `AddCardForm` below the lane header in `src/Kanban.Web/src/components/board/Lane.tsx`
- [X] T059 [US2] Update `BoardPage` to pass card data from `useBoard` into each `Lane`; wire `onCardCreated`/`onCardUpdated`/`onCardDeleted` callbacks in `src/Kanban.Web/src/pages/BoardPage.tsx`

**Checkpoint**: Member can add, edit, and delete cards via UI; all US2 unit, integration, and e2e tests green.

---

## Phase 5: User Story 3 — Member Reorders Cards and Lanes via Drag-and-Drop (Priority: P1)

**Goal**: Pointer and keyboard drag moves cards within and between lanes and reorders lanes. Changes
apply immediately in the UI (optimistic update) and persist to the server. Failed moves roll back
visually and show a persistent toast. Viewers cannot initiate drag. Concurrent moves to the same
card resolve with exactly one winner (409 for the loser).

**Independent Test**: Load board → drag card to new position → confirm immediate reorder → reload →
confirm persisted. Use keyboard (Space/arrow/Space) for the same moves. Viewer: drag is not
initiated. Concurrent: two simultaneous moves for same card → one 409.

### Tests for US3 (write FIRST — must fail before implementation)

- [ ] T060 [P] [US3] Write RTL component tests for `KanbanBoard` — renders with mocked sensors; `onDragEnd` for card dispatches `useMoveCard`; `onDragEnd` for lane dispatches `useMoveLane`; `DragOverlay` renders correct preview; announcements string present on `DndContext` — in `src/Kanban.Web/tests/components/KanbanBoard.test.tsx` (mock `@dnd-kit/core` sensors per dnd-kit docs)
- [ ] T061 [P] [US3] Write RTL component tests for `Lane` — renders as sortable container; card list renders in position order; `aria-describedby` present on draggable cards; keyboard instructions element present — in `src/Kanban.Web/tests/components/Lane.test.tsx`
- [ ] T062 [P] [US3] Write failing Playwright e2e tests for US3 acceptance scenarios 1–6 in `tests/e2e/DragDropTests.cs` — card same-lane reorder persists, card cross-lane move persists, lane reorder persists, keyboard drag (Space/arrow/Space), viewer drag not initiated, concurrent move 409 (parallel API calls)

### Implementation for US3

- [ ] T063 [P] [US3] Implement `useMoveCard` mutation hook with TanStack Query optimistic update pattern (`onMutate` snapshot, `onError` rollback + persistent toast, `onSettled` invalidate `['boards', boardId]`); include `expectedVersion` in request body in `src/Kanban.Web/src/hooks/useMoveCard.ts`
- [ ] T064 [P] [US3] Implement `useMoveLane` mutation hook with same optimistic update pattern as `useMoveCard`; include `expectedVersion` in request body in `src/Kanban.Web/src/hooks/useMoveLane.ts`
- [ ] T065 [US3] Implement `KanbanBoard` component — `DndContext` with `PointerSensor` + `KeyboardSensor` (no `MouseSensor`); `SortableContext` (horizontal) for lane IDs; `onDragEnd` dispatches to `useMoveLane` or `useMoveCard` based on active item type; `announcements` prop with "Picked up", "Moved to", "Dropped", "Cancelled" messages (WCAG AA); `DragOverlay` child at root in `src/Kanban.Web/src/components/board/KanbanBoard.tsx`
- [ ] T066 [P] [US3] Implement `CardDragPreview` and `LaneDragPreview` components for use inside `DragOverlay` in `src/Kanban.Web/src/components/board/CardDragPreview.tsx` and `LaneDragPreview.tsx`
- [ ] T067 [US3] Update `Lane` component — wrap with `useSortable`; add `SortableContext` (vertical) for card IDs; update `CardItem` to wrap with `useSortable`; add `aria-describedby` pointing to visually-hidden keyboard instructions; disable drag affordance when `callerRole === 'Viewer'` — in `src/Kanban.Web/src/components/board/Lane.tsx` and `CardItem.tsx`
- [ ] T068 [US3] Update `BoardPage` to render `KanbanBoard` (replacing plain lane list); pass `useMoveCard` and `useMoveLane` through to `KanbanBoard`; restore focus to moved item after successful drop in `src/Kanban.Web/src/pages/BoardPage.tsx`

**Checkpoint**: All three move types work with mouse and keyboard; optimistic rollback shows toast on API failure; concurrent move produces 409; all US3 RTL and e2e tests green.

---

## Phase 6: User Story 4 — Board Owner Invites and Manages Board Members (Priority: P2)

**Goal**: Board owners and admins can invite by email+role, change a member's role, and remove a
member. Invitation acceptance atomically creates the user (if new) AND adds the board membership.
The last Owner cannot be removed or downgraded. The invite response includes the board ID so the
frontend redirects the invitee directly to their board.

**Independent Test**: Owner invites new email → accept link creates user + member → verify member
list shows Member role → Owner changes role to Viewer → Viewer cannot add cards → Owner removes
member → member gets 404 on board access. Attempt to remove last Owner → 400.

### Tests for US4 (write FIRST — must fail before implementation)

- [ ] T069 [P] [US4] Write failing unit tests (10 tests) for `BoardMembershipService` — invite new pending member, invite existing pending (idempotent — returns same token), changeRole Owner→Member success, changeRole last-Owner → BusinessRuleException, remove member success, remove last-Owner → BusinessRuleException, list members ordered by joinedAt, viewer cannot invite 403, member cannot invite 403, change role to Owner success — in `tests/unit/Business/BoardMembershipServiceTests.cs`; use `BoardMemberBuilder`
- [ ] T070 [P] [US4] Write failing integration tests for `BoardMemberEndpoints` — `GET /members` (any member 200, non-member 404), `POST /invites` (owner 201, idempotent 200, member 403, viewer 403, already member 409), `PATCH /members/{userId}` (role change 200, last-owner 400), `DELETE /members/{userId}` (remove 204, last-owner 400); also test board-scoped `POST /invites/{token}/accept` returns `AcceptInviteResponseDto` with non-null `boardId` in `tests/integration/Api/BoardMemberEndpointTests.cs`
- [ ] T071 [P] [US4] Write failing Playwright e2e tests for US4 acceptance scenarios 1–7 in `tests/e2e/BoardMembershipTests.cs` — invite flow (new user created + redirected to board), existing user invite, role change to Viewer loses write access, remove member loses board access, last-owner guard 400, member cannot invite 403
- [ ] T072 [P] [US4] Write RTL component tests for `BoardMembersPanel` — renders member list with roles, invite form with email+role fields, role change dropdown for Owner, remove button for Owner, error state on last-owner attempt — in `src/Kanban.Web/tests/components/BoardMembersPanel.test.tsx`

### Implementation for US4

- [ ] T073 [US4] Implement `BoardMembershipService` (invite with idempotency check via `FindPendingInviteAsync`, list members, changeRole with `CountOwnersAsync` guard, remove with `CountOwnersAsync` guard) in `src/Kanban.Business/Services/BoardMembershipService.cs`; register in `Program.cs`
- [ ] T074 [US4] Extend `InvitationService.AcceptAsync` to inject `IBoardMemberRepository` — when `invitation.BoardId` is non-null, insert into `board_members` within the same deferred transaction as the user and invitation updates (atomic: User INSERT + board_members INSERT + invitation consumed_at UPDATE) in `src/Kanban.Business/Services/InvitationService.cs`
- [ ] T075 [US4] Update `POST /api/v1/invites/{token}/accept` endpoint to return `AcceptInviteResponseDto { User: CurrentUserDto, BoardId: Guid? }` instead of bare `CurrentUserDto`; update `InviteEndpoints.cs`; update frontend `useAcceptInvite` hook to read `.user` (for session) and `.boardId` (for redirect) — `boardId` null for system-level invites (001 flow unchanged) in `src/Kanban.Api/Endpoints/InviteEndpoints.cs` and `src/Kanban.Web/src/hooks/`
- [ ] T076 [P] [US4] Implement `BoardMemberEndpoints` (`GET /boards/{boardId}/members`, `POST /boards/{boardId}/invites`, `PATCH /boards/{boardId}/members/{userId}`, `DELETE /boards/{boardId}/members/{userId}`) in `src/Kanban.Api/Endpoints/BoardMemberEndpoints.cs`; register in `Program.cs`
- [ ] T077 [P] [US4] Implement `useBoardMembers` (query), `useInviteBoardMember`, `useChangeMemberRole`, `useRemoveBoardMember` (mutation) hooks with `onSettled` cache invalidation in `src/Kanban.Web/src/hooks/` (four files)
- [ ] T078 [US4] Implement `BoardMembersPanel` component — member list with role badge, invite form (email + role select + submit), role change dropdown for each member (Owner/Admin only), remove button (disabled if last owner); persistent toast on mutation error in `src/Kanban.Web/src/components/board/BoardMembersPanel.tsx`
- [ ] T079 [US4] Wire `BoardMembersPanel` into `BoardPage` — show/hide panel via toggle button (visible to Owner/Admin only); pass `callerRole` from `useBoard` data in `src/Kanban.Web/src/pages/BoardPage.tsx`

**Checkpoint**: Owner can invite, change roles, and remove members; atomic acceptance creates user + board member; all US4 unit, integration, and e2e tests green.

---

## Phase 7: User Story 5 — Viewer Browses a Board Read-Only (Priority: P3)

**Goal**: A user with the Viewer role sees all lanes and cards but cannot create, update, delete,
or drag anything. Backend authorization already enforces this via `BoardAuthorizationHandler`;
this phase ensures the frontend hides write controls for Viewers and validates the end-to-end
experience.

**Independent Test**: Assign Viewer role → sign in → board loads fully → every write attempt
(add card, edit card, delete card, add lane, rename lane, delete lane, drag card, drag lane)
is refused — either by the UI hiding the control or by returning 403 from the API.

### Implementation for US5

- [ ] T080 [US5] Update board UI components to conditionally hide write controls when `callerRole === 'Viewer'` — hide "Add Card" button, hide drag handles, hide edit/delete card buttons, hide "Add Lane" button, hide rename/delete lane controls, disable `CardItem` and `Lane` sortable interaction; pass `callerRole` as prop down from `BoardPage` → `KanbanBoard` → `Lane` → `CardItem` in `src/Kanban.Web/src/components/board/KanbanBoard.tsx`, `Lane.tsx`, `CardItem.tsx`, `AddCardForm.tsx`, `AddLaneForm.tsx`
- [ ] T081 [P] [US5] Write Playwright e2e tests for US5 acceptance scenarios 1–2 in `tests/e2e/BoardManagementTests.cs` — sign in as Viewer-role user, confirm board and all cards visible, confirm no write controls visible, confirm direct API write attempts return 403 (not just hidden controls)

**Checkpoint**: Viewer experience is enforced in both UI and API; US5 e2e scenarios pass.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation across all stories — accessibility audit, bundle size, and quickstart
checklist confirmation.

- [ ] T082 [P] Audit all new C# public methods added in Phases 1–7 for FluentValidation guard coverage (constitution v1.8.0 pattern — `InlineValidator<T>` or `AbstractValidator<T>` with `.ValidateAndThrow()` on non-nullable non-optional params); fix any gaps; run `dotnet build` with zero warnings
- [ ] T083 [P] Verify WCAG AA compliance — `DndContext` `announcements` prop covers pick-up/move/drop/cancel messages; `aria-describedby` present on all draggable items; `getByRole('heading', { level: 1 })` present on `BoardListPage` and `BoardPage` (RTL assertion); keyboard-only Playwright test completes successfully
- [ ] T084 [P] Run frontend production build and verify initial JS bundle ≤ 300 KB gzipped using `npm run build` in `src/Kanban.Web/`; confirm `BoardPage` and board list split into separate route chunks via `React.lazy()`
- [ ] T085 Run all four test layers and confirm 100% green — `dotnet test tests/unit/`, `dotnet test tests/integration/`, `npm test -- --run` in `src/Kanban.Web/`, `npx playwright test` in `tests/e2e/`; automated coverage must be ≥ 90%
- [ ] T086 [P] Run the quickstart.md verified feature checklist (11 items) end-to-end on a clean DB — board creation, lane ordering, card drag (mouse + keyboard), Viewer permission denial, non-member 404, board invitation acceptance, concurrent move 409
- [ ] T087 [P] Verify all six RTL test files pass and assert `getByRole` (not `getByTestId`) throughout: `BoardListPage.test.tsx`, `BoardPage.test.tsx`, `KanbanBoard.test.tsx`, `Lane.test.tsx`, `CardItem.test.tsx`, `BoardMembersPanel.test.tsx`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Validation Unification (Phase 0)**: No dependencies — start immediately; BLOCKS all other phases (FluentValidation must be the validation mechanism before any new service code is written)
- **Setup (Phase 1)**: Depends on Phase 0 checkpoint passing
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user story phases
- **US1 (Phase 3)**: Depends on Phase 2 — no story dependencies
- **US2 (Phase 4)**: Depends on Phase 2 — no story dependencies (US1 board+lane infrastructure needed in DB, not code)
- **US3 (Phase 5)**: Depends on US1 (Lane component exists) and US2 (CardItem exists) — adds sortable behavior
- **US4 (Phase 6)**: Depends on Phase 2 — no US1/US2/US3 code dependency (separate service layer)
- **US5 (Phase 7)**: Depends on US1–US4 (UI components must exist to apply role guards)
- **Polish (Phase 8)**: Depends on all story phases complete

### User Story Dependencies

```
Phase 0 (Validation Unification — US6) ← start here; BLOCKS everything
    └── Phase 1 (Setup)
            └── Phase 2 (Foundational)
                    ├── Phase 3 (US1) ──┐
                    ├── Phase 4 (US2) ──┤
                    │                   ▼
                    │           Phase 5 (US3) ── depends on US1 Lane + US2 CardItem components
                    └── Phase 6 (US4)
                                │
                                ▼
                        Phase 7 (US5) ── depends on all UI components existing
                                │
                                ▼
                        Phase 8 (Polish)
```

### Within Each User Story

1. Write failing tests (unit + integration + RTL + e2e) — COMMIT (red)
2. Implement until all tests pass — COMMIT (green)
3. Refactor if needed — COMMIT (refactor)
4. Run checkpoint validation before moving to next story

### Parallel Opportunities

All tasks marked [P] within the same phase can run in parallel.

**Phase 1 parallel batch (T002–T015)**: All 14 tasks are independent file creations.

**Phase 2 parallel batch (T016–T028)**: Interfaces, transforms, validators, and BoardOperations/Requirement are all independent. T029 (BoardAuthorizationHandler) depends on T019 (IBoardMemberRepository). T030 (DI registration) depends on all Phase 2 tasks.

**Phase 3 parallel batch within tests (T031–T035)**: All five test files are independent.

**Phase 3 parallel batch within implementation**: T038 and T039 (endpoints) are independent of each other. T042, T043, T044 (hooks) are independent of each other. T045 (components) is independent.

---

## Parallel Example: User Story 1 Tests

```
# All five test files can be written simultaneously:
Task T031: tests/unit/Business/BoardServiceTests.cs
Task T032: tests/unit/Business/LaneServiceTests.cs
Task T033: tests/integration/Api/BoardEndpointTests.cs
Task T034: tests/integration/Api/LaneEndpointTests.cs
Task T035: tests/e2e/BoardManagementTests.cs
```

## Parallel Example: User Story 1 Frontend Hooks

```
# All three hook groups can be written simultaneously:
Task T042: useBoards.ts + useBoard.ts
Task T043: useCreateBoard.ts + useDeleteBoard.ts
Task T044: useCreateLane.ts + useRenameLane.ts + useDeleteLane.ts
Task T045: BoardCard.tsx + CreateBoardDialog.tsx
```

---

## Implementation Strategy

### MVP First (P1 Stories Only — US1 + US2 + US3)

1. Complete Phase 1 (Setup)
2. Complete Phase 2 (Foundational) — CRITICAL gate
3. Complete Phase 3 (US1) → validate admin can create board + lanes
4. Complete Phase 4 (US2) → validate member can manage cards
5. Complete Phase 5 (US3) → validate drag-and-drop reorder works
6. **STOP and DEMO**: Three P1 stories constitute a functional Kanban board — boards, cards, reordering

### Full Delivery (Add P2 + P3)

7. Complete Phase 6 (US4) → board membership and invitation flow
8. Complete Phase 7 (US5) → viewer read-only enforcement
9. Complete Phase 8 (Polish) → all tests pass, bundle size in budget, quickstart verified

### Parallel Team Strategy

With two developers after Phase 2 completes:

- **Developer A**: Phase 3 (US1) → Phase 5 (US3 — builds on US1 Lane component)
- **Developer B**: Phase 4 (US2) → Phase 6 (US4 — independent service)
- Merge after US3 and US4 are both complete → Phase 7 (US5) → Phase 8 (Polish)

---

## Summary

| Phase | Story | Tasks | Test Files |
|-------|-------|-------|------------|
| 0 — Validation Unification | US6 | T088–T100 (13) | UserTests, InvitationTests, InvitationTokenTests, ValidationExceptionMappingTests |
| 1 — Setup | — | T001–T015 (15) | — |
| 2 — Foundational | — | T016–T030 (15) | — |
| 3 — US1 Board + Lane | P1 | T031–T047 (17) | BoardServiceTests, LaneServiceTests, BoardEndpointTests, LaneEndpointTests, BoardManagementTests, BoardListPage.test, BoardPage.test |
| 4 — US2 Card Management | P1 | T048–T059 (12) | CardServiceTests, CardEndpointTests, CardManagementTests, CardItem.test |
| 5 — US3 Drag-and-Drop | P1 | T060–T068 (9) | DragDropTests, KanbanBoard.test, Lane.test |
| 6 — US4 Membership | P2 | T069–T079 (11) | BoardMembershipServiceTests, BoardMemberEndpointTests, BoardMembershipTests, BoardMembersPanel.test |
| 7 — US5 Viewer | P3 | T080–T081 (2) | BoardManagementTests (extended) |
| 8 — Polish | — | T082–T087 (6) | All layers |
| **Total** | | **100 tasks** | **20 test files** |

**MVP scope**: Complete Phase 0 first (US6 prerequisite), then Phases 1–5 (US1 + US2 + US3) = 81 tasks deliver a fully functional Kanban board.
Phases 6–8 add collaboration and complete the spec.
