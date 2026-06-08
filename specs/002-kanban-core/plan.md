# Implementation Plan: Kanban Core

**Branch**: `002-kanban-core` | **Date**: 2026-06-01 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-kanban-core/spec.md`

## Summary

Delivers the full Kanban board experience on top of the identity foundation established in
001-auth-onboarding: boards (admin-created), ordered lanes, ordered cards, keyboard-accessible
drag-and-drop reordering, and board-scoped membership management with role-based authorization.

Technical approach: Four new aggregate roots (`Board`, `Lane`, `Card`, `BoardMember`) follow
the established Repository + Business Service layering. Integer gapless positions are updated
atomically inside deferred SQLite transactions with Polly retry. Optimistic concurrency on card
and lane moves is enforced via a `version` column — concurrent move losers receive 409 rather
than silently overwriting. Resource-based authorization (`BoardOperations` + `BoardMembershipRequirement` + `BoardAuthorizationHandler`) enforces board-role rules in the
Business layer. Board-scoped invitations extend the existing token mechanism by adding nullable
`board_id` / `board_role` columns to the `invitations` table; acceptance creates both the User
record (if new) and the BoardMember record in one atomic transaction. The React frontend uses
dnd-kit (`PointerSensor` + `KeyboardSensor`) with TanStack Query optimistic updates.

## Technical Context

**Language/Version**: C# (.NET 10), TypeScript (React latest stable / Vite)

**Primary Dependencies** (additions to 001 baseline):
- Backend (new): no new packages required — all needed packages from 001 (Dapper, DbUp,
  FluentValidation, Polly, `Asp.Versioning.Http`, `Microsoft.AspNetCore.Authorization`) are
  already referenced
- Frontend (new): `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities`

**Storage**: SQLite (local dev + CI), two new DbUp migrations (003, 004); `IDbConnection`
abstraction unchanged

**Testing**: xUnit + FluentAssertions (unit + integration), React Testing Library + Vitest
(component), Playwright (e2e) — all tooling from 001, no additions required

**Target Platform**: Local development machine — same as 001

**Project Type**: Web application — REST API (ASP.NET 10) + SPA (React/Vite)

**Performance Goals**:
- Board load (10 lanes × 50 cards) < 3 s (SC-004)
- Drag move UI feedback < 100 ms from release (SC-002, SC-003)
- Server confirms move < 2 s (SC-002, SC-003)
- API responses < 200 ms p95 (constitution baseline)

**Constraints**: Local demo only; no new external infrastructure

**Scale/Scope**: MVP — single admin, small number of invited users, single-replica SQLite

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Spec-First (I) | ✅ PASS | `spec.md` complete; quality checklist all items pass |
| TDD mandatory (II) | ✅ PASS | All four test layers planned; red-before-green in task order |
| Simplicity (III) | ✅ PASS | No new packages beyond dnd-kit (already in constitution); no abstractions beyond spec |
| Security-First (IV) | ✅ PASS | Board-role authorization enforced in Business layer; enumeration prevention (404 for non-members); version-column concurrency; no secrets introduced |
| SOLID (V) | ✅ PASS | One service per aggregate root; DI throughout; BoardAuthorizationHandler is single-responsibility |
| Self-Documenting (VI) | ✅ PASS | No in-class comments; names express intent (BoardOperations, FindBoardForMemberAsync, etc.) |
| Design Patterns (VII) | ✅ PASS | Repository (DataAccess), Service + Transforms (Business), Adapter (unchanged), Resource-Based Auth (BoardAuthorizationHandler), Container/Presentational + custom hooks (React) |
| MVP-Oriented (VIII) | ✅ PASS | P1 stories (board + lane + card + DnD) are minimum viable Kanban; P2 (membership) and P3 (viewer) layered on |
| NFRs (IX) | ✅ PASS | Performance targets from SC-001–SC-010; WCAG AA on drag-drop (keyboard + announcements); version-column concurrency |
| Entities never cross API boundary | ✅ PASS | Domain entities stay in Business; only `Kanban.Contracts` DTOs exit the API |
| RegisteredUser policy gate | ✅ PASS | All new endpoints require RegisteredUser; board-level operations additionally require board role |
| FluentValidation validators in non-API public methods | ✅ PASS | All new Business, DataAccess, Domain, AntiCorruption public methods use FluentValidation AbstractValidator/InlineValidator; US6 migrates existing Verify usages |
| gitleaks pre-commit hook | ✅ PASS | Already installed from 001; no change |
| DDD aggregate roots | ✅ PASS | `Board`, `Lane`, `Card` are aggregate roots; `BoardMember` is not (accessed via Board/User context) |
| IDbConnection only in DataAccess | ✅ PASS | All new repositories depend on `IDbConnection`; no `SqliteConnection` outside DI |
| Deferred transactions + Polly | ✅ PASS | Position updates use deferred transactions; Polly wraps SQLITE_BUSY retries; version mismatch → 409 (not retry) |
| MVP scoping (Implementation Order) | ✅ PASS | All referenced sections are category (a) "implement now"; dnd-kit promoted from (b) to (a) — justified below |

**Complexity Tracking note**: dnd-kit is promoted from category (b) (wire the seam) to (a) (implement now) because drag-and-drop is P1 in the spec — a Kanban board that cannot reorder cards is not a Kanban board. Justification entered in Complexity Tracking.

Post-Phase-1 re-check: see bottom of this file.

## Project Structure

### Documentation (this feature)

```text
specs/002-kanban-core/
├── plan.md              ← this file
├── research.md          ← Phase 0: position strategy, concurrency, invitation extension, DnD, auth
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
│   ├── Auth/
│   │   ├── BoardOperations.cs               # NEW — static class of operation constants
│   │   ├── BoardMembershipRequirement.cs    # NEW
│   │   └── BoardAuthorizationHandler.cs     # NEW — resolves board role from DB, evaluates requirement
│   └── Endpoints/
│       ├── BoardEndpoints.cs                # NEW
│       ├── LaneEndpoints.cs                 # NEW
│       ├── CardEndpoints.cs                 # NEW
│       └── BoardMemberEndpoints.cs          # NEW
│   (Program.cs extended — new DI registrations, new endpoint groups)
│
├── Kanban.Web/
│   └── src/
│       ├── pages/
│       │   ├── BoardListPage.tsx            # NEW — lists user's boards
│       │   └── BoardPage.tsx               # NEW — full board view
│       ├── components/
│       │   ├── boards/
│       │   │   ├── BoardCard.tsx            # NEW — board summary card in list
│       │   │   └── CreateBoardDialog.tsx    # NEW — admin-only create dialog
│       │   └── board/
│       │       ├── KanbanBoard.tsx          # NEW — DndContext root; owns onDragEnd dispatch
│       │       ├── Lane.tsx                 # NEW — sortable lane container
│       │       ├── CardItem.tsx             # NEW — sortable card item
│       │       ├── CardDragPreview.tsx      # NEW — DragOverlay card
│       │       ├── LaneDragPreview.tsx      # NEW — DragOverlay lane
│       │       ├── AddLaneForm.tsx          # NEW — inline form
│       │       ├── AddCardForm.tsx          # NEW — inline form
│       │       ├── CardDetailDialog.tsx     # NEW — view/edit card details
│       │       └── BoardMembersPanel.tsx    # NEW — member list + invite + role management
│       └── hooks/
│           ├── useBoards.ts                 # NEW — ['boards'] query
│           ├── useBoard.ts                  # NEW — ['boards', boardId] query
│           ├── useCreateBoard.ts            # NEW
│           ├── useDeleteBoard.ts            # NEW
│           ├── useCreateLane.ts             # NEW
│           ├── useRenameLane.ts             # NEW
│           ├── useDeleteLane.ts             # NEW
│           ├── useMoveLane.ts               # NEW — includes optimistic update + version
│           ├── useCreateCard.ts             # NEW
│           ├── useUpdateCard.ts             # NEW
│           ├── useDeleteCard.ts             # NEW
│           ├── useMoveCard.ts               # NEW — includes optimistic update + version
│           ├── useBoardMembers.ts           # NEW
│           ├── useInviteBoardMember.ts      # NEW
│           ├── useChangeMemberRole.ts       # NEW
│           └── useRemoveBoardMember.ts      # NEW
│
├── Kanban.Business/
│   ├── Services/
│   │   ├── BoardService.cs                  # NEW — create, list, get, delete
│   │   ├── LaneService.cs                   # NEW — create, rename, reorder, delete
│   │   ├── CardService.cs                   # NEW — create, update, move, delete
│   │   └── BoardMembershipService.cs        # NEW — invite, list, changeRole, remove
│   │   (InvitationService.cs extended — AcceptAsync creates BoardMember when board_id set)
│   └── Interfaces/
│       ├── IBoardService.cs                 # NEW
│       ├── ILaneService.cs                  # NEW
│       ├── ICardService.cs                  # NEW
│       └── IBoardMembershipService.cs       # NEW
│   └── Transforms/
│       ├── BoardTransforms.cs               # NEW
│       ├── LaneTransforms.cs                # NEW
│       ├── CardTransforms.cs                # NEW
│       └── BoardMemberTransforms.cs         # NEW
│
├── Kanban.Domain/
│   ├── Entities/
│   │   ├── Board.cs                         # NEW — aggregate root
│   │   ├── Lane.cs                          # NEW — aggregate root
│   │   ├── Card.cs                          # NEW — aggregate root
│   │   ├── BoardMember.cs                   # NEW
│   │   └── CardAssignee.cs                  # NEW (data model only; no service in this feature)
│   │   (Invitation.cs extended — adds Guid? BoardId, BoardRole? BoardRole properties)
│   └── Enums/
│       └── BoardRole.cs                     # NEW — Owner, Member, Viewer
│
├── Kanban.Contracts/
│   ├── BoardSummaryDto.cs                   # NEW
│   ├── BoardDetailDto.cs                    # NEW
│   ├── LaneDto.cs                           # NEW
│   ├── CardDto.cs                           # NEW
│   ├── BoardMemberDto.cs                    # NEW
│   ├── BoardRoleDto.cs                      # NEW — enum
│   ├── AcceptInviteResponseDto.cs           # NEW — replaces bare CurrentUserDto on accept
│   ├── CreateBoardRequest.cs                # NEW
│   ├── CreateLaneRequest.cs                 # NEW
│   ├── RenameLaneRequest.cs                 # NEW
│   ├── MoveLaneRequest.cs                   # NEW
│   ├── CreateCardRequest.cs                 # NEW
│   ├── UpdateCardRequest.cs                 # NEW
│   ├── MoveCardRequest.cs                   # NEW
│   ├── InviteBoardMemberRequest.cs          # NEW
│   └── ChangeMemberRoleRequest.cs           # NEW
│
├── Kanban.Data/
│   └── migrations/
│       ├── sqlite/
│       │   ├── 003_boards_lanes_cards.sql   # NEW
│       │   └── 004_extend_invitations.sql   # NEW
│       └── postgres/
│           ├── 003_boards_lanes_cards.sql   # NEW
│           └── 004_extend_invitations.sql   # NEW
│
└── Kanban.DataAccess/
    ├── Interfaces/
    │   ├── IBoardRepository.cs              # NEW
    │   ├── ILaneRepository.cs               # NEW
    │   ├── ICardRepository.cs               # NEW
    │   └── IBoardMemberRepository.cs        # NEW
    └── Repositories/
        ├── BoardRepository.cs               # NEW — FindBoardForMemberAsync (membership + null guard)
        ├── LaneRepository.cs                # NEW — includes position-shift queries
        ├── CardRepository.cs                # NEW — includes position-shift queries; multi-row board query
        └── BoardMemberRepository.cs         # NEW

tests/
├── unit/
│   ├── Builders/
│   │   ├── BoardBuilder.cs                  # NEW
│   │   ├── LaneBuilder.cs                   # NEW
│   │   ├── CardBuilder.cs                   # NEW
│   │   └── BoardMemberBuilder.cs            # NEW
│   └── Business/
│       ├── BoardServiceTests.cs             # NEW — 8 tests: create, list, get, delete, auth
│       ├── LaneServiceTests.cs              # NEW — 10 tests: CRUD, position invariants, name uniqueness
│       ├── CardServiceTests.cs              # NEW — 12 tests: CRUD, move same lane, move cross lane, version conflict, position invariants
│       └── BoardMembershipServiceTests.cs   # NEW — 10 tests: invite, accept (new user), accept (existing), changeRole, remove, last-owner guard
│
├── integration/
│   └── Api/
│       ├── BoardEndpointTests.cs            # NEW — all 4 board endpoints, auth scenarios
│       ├── LaneEndpointTests.cs             # NEW — all 5 lane endpoints, auth scenarios
│       ├── CardEndpointTests.cs             # NEW — all 5 card endpoints, concurrent move test
│       └── BoardMemberEndpointTests.cs      # NEW — invite, accept (board-scoped), role change, remove, last-owner guard
│
└── e2e/
    ├── BoardManagementTests.cs              # NEW — US1 acceptance scenarios 1–6
    ├── CardManagementTests.cs               # NEW — US2 acceptance scenarios 1–6
    ├── DragDropTests.cs                     # NEW — US3 acceptance scenarios 1–6
    └── BoardMembershipTests.cs              # NEW — US4 acceptance scenarios 1–7

src/Kanban.Web/tests/
└── components/
    ├── BoardListPage.test.tsx               # NEW
    ├── BoardPage.test.tsx                   # NEW
    ├── KanbanBoard.test.tsx                 # NEW — dnd-kit sensors mocked
    ├── Lane.test.tsx                        # NEW
    ├── CardItem.test.tsx                    # NEW
    └── BoardMembersPanel.test.tsx           # NEW
```

**Structure Decision**: Web application layout unchanged from 001. All new files follow the
established eight-project pattern. Frontend components are organized under `components/boards/`
(list views) and `components/board/` (single-board view with DnD).

## Complexity Tracking

| Complexity | Why Needed | Simpler Alternative Rejected Because |
|------------|-----------|-------------------------------------|
| dnd-kit promoted from (b) to (a) | Drag-and-drop is P1 in the spec (US3). A Kanban board without reordering is a static todo list | Constitution categories are for MVP scope decisions; if a P1 user story requires a feature, that feature is in scope |
| `DevEndpoints.cs` in `Kanban.Api` uses `IDbConnection` and `SystemRole` directly (violates layer rules) | Playwright e2e tests need a dev-only endpoint to seed DB state and issue auth cookies without Google OAuth; a Business layer wrapper adds indirection with zero benefit for dev-only infrastructure | Move to a DevService in Kanban.Business: adds a full service/interface pair for code that is stripped from production — net complexity increase, not a decrease |
| InvitationService extended with IBoardMemberRepository | Board-scoped invitation acceptance must create both the User and the BoardMember in one atomic transaction. InvitationService already owns the accept transaction boundary | Separate domain event: correct architecture but adds event infrastructure for one consumer; premature for MVP |
| `board_id` denormalized onto `cards` | Membership check for card operations requires board_id; adding a JOIN to lanes on every card operation is an N+1 pattern in disguise | Derive board_id from lane JOIN on every call: acceptable for low volume but violates the performance goal for boards with 50+ cards |
| `BoardAuthorizationHandler` injects IBoardMemberRepository | Resource-based auth must resolve the caller's board role from the DB; this is the only correct place to inject the repository for this purpose | Cache board role in a claim: roles change dynamically (an owner can be demoted); stale claims are a correctness bug |

## Post-Phase-1 Constitution Re-check

| Gate | Status | Notes |
|------|--------|-------|
| All Phase 0 gates | ✅ PASS | Unchanged |
| Data model cross-boundary check | ✅ PASS | Domain entities → DTOs in Business/Transforms; no entities in Contracts |
| No `RETURNING` in INSERT | ✅ PASS | Insert + separate SELECT/query-after-insert pattern used throughout |
| Concurrent move safety | ✅ PASS | version column + `UPDATE WHERE version = @expected` → 0 rows → 409; Polly wraps SQLITE_BUSY only |
| Position gaplessness after all mutations | ✅ PASS | Batch shift UPDATE inside deferred transaction on every create, delete, reorder |
| Board enumeration prevention | ✅ PASS | `FindBoardForMemberAsync` returns null for non-members; null → NotFoundException("board.not_found") |
| Last Owner guard | ✅ PASS | `CountBoardOwners(boardId)` checked before every role change and member removal; 0 remaining owners → 400 |
| Board invitation atomic outcome | ✅ PASS | User INSERT + board_members INSERT + invitation consumed_at UPDATE in single deferred transaction |
| ON DELETE CASCADE correctness | ✅ PASS | `PRAGMA foreign_keys = ON` already set in SqliteConnectionFactory; lanes cascade to cards, boards cascade to lanes + board_members |
| DnD keyboard accessibility | ✅ PASS | KeyboardSensor on DndContext; announcements on DndContext; aria-describedby on draggables; focus restored after drop |
| RTL component tests for dnd-kit | ✅ PASS | Sensors mocked in RTL tests (dnd-kit recommends sensor mocking for component tests); Playwright owns the actual drag behavior |
| `AcceptInviteResponseDto` backward compatibility | ✅ PASS | New DTO wraps `CurrentUserDto`; frontend always reads `.user` field; `boardId` null for system-level invites (001 flow unchanged) |
