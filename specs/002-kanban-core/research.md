# Research: Kanban Core — Board, Lane, Card, and Membership Management

**Phase**: 0 | **Branch**: `002-kanban-core`

---

## Decision 1: Position Management Strategy

**Decision**: Integer gapless positions (1, 2, 3, …) per the constitution. Reorder via batch
UPDATE that shifts affected siblings in a single deferred transaction; no gap-based or
fractional indexing.

**Rationale**:
- Constitution mandates: "Lane positions are unique per Board; Card positions are unique per
  Lane. When a Card or Lane is moved, all affected sibling positions MUST be updated in a
  single transaction. There is no gap-based or fractional indexing."
- For a reorder (e.g., move item from position 5 to position 2):
  - `UPDATE … SET position = position + 1 WHERE … AND position >= 2 AND position < 5`
  - `UPDATE … SET position = 2 WHERE id = ?`
  - Both UPDATEs run inside a single deferred transaction
- For inserting at the bottom: SELECT MAX(position) + 1 inside the same transaction
- For deletion: shift items above the deleted position down by 1

**Alternatives considered**:
- **Fractional/gap-based indexing** (e.g., Lexorank): avoids multi-row UPDATEs on every move
  but introduces irrational space, periodic rebalancing complexity, and client-generated
  ordering values that are harder to validate. Rejected per constitution Principle III.
- **Large integer gaps** (e.g., multiples of 100): defers but doesn't eliminate the
  rebalancing problem. Rejected per constitution explicit requirement.

**Concurrency note**: See Decision 2 for how concurrent moves to the same list are resolved.

---

## Decision 2: Concurrent Move Safety — Version Column + Optimistic Concurrency

**Decision**: Cards and Lanes each carry an integer `version` column that increments on every
positional change. Move requests from the client include the `expectedVersion`. If the server's
UPDATE WHERE version = expectedVersion returns 0 rows, the endpoint returns 409 Conflict.

**Rationale**:
- SC-006: "Across 20 simultaneous move attempts on the same card, exactly one succeeds and
  the rest receive a conflict response."
- SQLite is single-writer, so concurrent moves serialize naturally at the DB level. The version
  column provides the detection mechanism: if two clients both saw version 3 and both attempt
  to move the same card, the second UPDATE WHERE version = 3 returns 0 rows after the first
  commit increments the version to 4.
- Version check + position shift all run inside a single deferred transaction via Polly retry.
  The Polly retry is for SQLITE_BUSY (lock contention during position shifting) — not for
  version conflicts (those are explicit 409s, not transient errors).

**Alternatives considered**:
- **Last-write-wins** (no version check): simpler but violates SC-006's requirement to return
  a conflict response to losers.
- **Timestamp-based optimistic lock** (`updated_at`): same semantics but timestamp ties are
  theoretically possible in a fast system; integer version is strictly monotonic.

**Client contract**: Move request DTOs (`MoveCardRequest`, `MoveLaneRequest`) include an
`expectedVersion` integer. Frontend TanStack Query hooks read the version from the board
query cache and include it in move mutations.

---

## Decision 3: Board Invitation Architecture — Extending Invitations Table

**Decision**: The existing `invitations` table gains two nullable columns: `board_id`
(FK to `boards`) and `board_role` (text). A null `board_id` indicates a system-level
invitation (001 behaviour). A non-null `board_id` indicates a board-scoped invitation.
`InvitationService.AcceptAsync` is extended to create a `board_members` record when
`board_id` is set, within the same deferred transaction as user creation.

**Rationale**:
- Spec assumption: "The invitation acceptance flow from the auth spec is extended for board
  invitations: board invitations reuse the same token and acceptance mechanism but additionally
  create the board membership record on acceptance."
- Reusing the existing token, hash, and expiry infrastructure avoids duplicating the token
  generation logic (Decision 1 in 001-auth-onboarding research.md).
- Single `POST /api/v1/invites/{token}/accept` endpoint handles both invitation types.
  The endpoint does not need to know whether the invitation is user-level or board-level —
  the InvitationService reads the invitation and handles both cases.
- The `InvitationService.AcceptAsync` method's single atomic transaction (user INSERT +
  board_members INSERT + invitation consumed_at UPDATE) satisfies FR-017's "single atomic
  outcome" requirement.

**SOLID note (Complexity Tracking)**: `InvitationService` gains a dependency on
`IBoardMemberRepository` for the board-scoped acceptance path. This is documented in the
Complexity Tracking table. The alternative (a separate event-driven approach) is deferred as
over-engineering for MVP.

**Alternatives considered**:
- **Separate `board_invitations` table**: clean separation but duplicates token generation,
  hash storage, and acceptance endpoint logic. Rejected (YAGNI / Principle III).
- **Domain event on acceptance**: `InvitationAccepted` event triggers membership creation in
  a handler. Correct architecture for a larger system but adds event infrastructure for one
  consumer. Deferred — not required for MVP.

---

## Decision 4: Board Data Loading — Single Query with Multi-Row Mapping

**Decision**: `GET /api/v1/boards/{boardId}` returns a fully-hydrated `BoardDetailDto` with
lanes and cards in one Dapper multi-row-mapping query (LEFT JOIN boards → lanes → cards),
ordered by lane.position, card.position.

**Rationale**:
- SC-004: board with 10 lanes × 50 cards must load in under 3 seconds. A single query with
  ordered results avoids N+1 (10 lane queries + 10 × 50 card queries = 510 DB round trips).
- Dapper's `QueryAsync<dynamic>` + manual mapping, or `QueryAsync` with a split-on pattern,
  handles the 1:N:N join in one pass.
- For the list view (`GET /api/v1/boards`), a lighter query counts lanes and cards without
  fetching their content.

**Alternatives considered**:
- **Separate endpoints per resource layer** (`/boards/{id}/lanes`, `/lanes/{id}/cards`):
  clean REST but forces N+1 round trips from the frontend unless the client batches with
  TanStack Query's parallel queries. Rejected for board page load; still used for individual
  mutations.
- **GraphQL**: eliminates over-fetching but adds schema + resolver infrastructure. Rejected
  per Principle III.

---

## Decision 5: Drag-and-Drop Architecture — dnd-kit with DragOverlay

**Decision**: Use `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities` per constitution.
`KanbanBoard` wraps the entire board in a single `DndContext` with `PointerSensor` +
`KeyboardSensor`. Card dragging uses `SortableContext` per lane with
`verticalListSortingStrategy`. Lane dragging uses a wrapping `SortableContext` with
`horizontalListSortingStrategy`. A single `DragOverlay` at the `DndContext` root renders
the visual drag preview.

**Rationale**:
- Constitution specifies dnd-kit as the mandatory package with these exact sensors.
- Single `DndContext` at the board level handles both card-between-lanes and lane reorder in
  `onDragEnd`, distinguishing by checking whether `activeType` is `"card"` or `"lane"`.
- `DragOverlay` at root avoids stacking context issues with Fluent UI portals (per constitution).
- Optimistic update via TanStack Query `onMutate`/`onError`/`onSettled` per constitution:
  snapshot → apply → on error restore + toast.

**onDragEnd logic**:
```
if (activeType === "lane"):
  → moveLane(activeId, overLaneId)  [fires useMoveLane mutation]
if (activeType === "card"):
  if (sourceLaneId === destinationLaneId):
    → moveCard(activeId, sourceLaneId, newPosition)
  else:
    → moveCard(activeId, destinationLaneId, newPosition)
```

**Accessibility (WCAG AA)**:
- `DndContext.announcements`: "Picked up [card title]. Current position: [N] of [total]",
  "Moved [card title] to lane [name] at position [N]", "Dropped", "Move cancelled."
- `aria-describedby` on each draggable pointing to a `<span className="sr-only">` with
  keyboard instructions.
- Focus returns to the moved item after a successful keyboard drop.

---

## Decision 6: Board Authorization Architecture — Resource-Based Authorization

**Decision**: `BoardOperations` static class defines the operations. `BoardMembershipRequirement`
+ `BoardAuthorizationHandler` resolve the requesting user's board role from the DB (via
`IBoardMemberRepository`) and evaluate the requirement. Business layer service methods call
`IAuthorizationService.AuthorizeAsync` with a `BoardContext` — never the endpoint handlers
directly.

**Rationale**:
- Constitution mandates: "Enforcement: Business layer service methods call
  `IAuthorizationService.AuthorizeAsync` — never in endpoint handlers."
- `BoardContext` is a lightweight value type `{ Guid BoardId, BoardRole ResolvedRole }` loaded
  once per service call, not per request, avoiding a DB hit on every middleware invocation.
- Non-members receive a `NotFoundException("board.not_found")` before any board data is
  returned (FR-004, SC-010). The `BoardRepository.FindForUserAsync` query joins board to
  board_members and returns null when the user is not a member — the board itself is never
  loaded separately.

**Enumeration prevention**: `FindBoardForMemberAsync(boardId, userId)` returns null when
the user is not a member OR when the board does not exist. Business layer maps null to
`NotFoundException`. Endpoint never sees 403 for non-members — always 404.

---

## Decision 7: `board_id` Denormalization on Cards

**Decision**: The `cards` table stores a `board_id` column (FK to `boards`) alongside the
`lane_id` FK. This is a deliberate denormalization.

**Rationale**:
- The primary membership check for card operations needs the board_id. Without denormalization,
  every card-level operation requires an extra JOIN to `lanes` to get the board_id before
  checking board membership.
- `board_id` is immutable once a card is created (cards cannot be moved between boards).
  Denormalization is therefore safe — no update anomalies.
- All card inserts set board_id at creation time from the lane's board_id (read in the same
  transaction). A DB-level `CHECK` constraint is not feasible cross-table in SQLite without
  triggers, but the application layer enforces consistency at write time.

**Alternatives considered**:
- Join lanes on every card operation: adds a query per operation, degrading performance for
  high-throughput card moves. Rejected.

---

## Decision 8: Lane and Card Deletion Cascade

**Decision**: `lanes` → `cards` is `ON DELETE CASCADE` in the schema. `boards` → `lanes` is
`ON DELETE CASCADE`. `boards` → `board_members` is `ON DELETE CASCADE`. SQLite enforces these
with `PRAGMA foreign_keys = ON` (already set in the connection factory). The DELETE statement
targets the top-level entity; cascades handle the rest.

**Rationale**:
- FR-007: "Deleting a lane MUST also delete all cards it contains in a single atomic operation."
- FR-003: "Deletion MUST atomically remove all lanes, cards, memberships, and card assignments."
- `ON DELETE CASCADE` in the schema is simpler and more reliable than application-level
  multi-table deletes, especially under concurrent load.
- The `board_id` denormalization on cards means the cascade correctly handles all card records.

**SQLite prerequisite**: `PRAGMA foreign_keys = ON` is already applied in
`SqliteConnectionFactory` (Program.cs line 72). Cascade deletes will work correctly.
