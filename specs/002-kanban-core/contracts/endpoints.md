# API Endpoint Contracts: Kanban Core

**Phase**: 1 | **Branch**: `002-kanban-core`

All endpoints require the `RegisteredUser` policy unless stated otherwise.
All endpoints are in the `/api/v1` versioned group.
All error responses follow RFC 7807 Problem Details with a `code` extension field.

---

## Boards

### GET /api/v1/boards

List all boards the calling user is a member of (any role).

- **Auth**: RegisteredUser
- **Rate limit**: authenticated
- **Response 200**: `BoardSummaryDto[]` — ordered by `name` ascending
- **Response 401**: unauthenticated

---

### POST /api/v1/boards

Create a new board. Admin-only.

- **Auth**: Admin policy
- **Rate limit**: mutating
- **Request body**: `CreateBoardRequest { name: string }`
- **Response 201**: `BoardSummaryDto` — the created board; `Location` header omitted (not needed for MVP)
- **Response 403**: caller is not an admin — `code: "board.forbidden"`
- **Response 409**: a board with that name already exists — `code: "board.name_conflict"`
- **Response 422**: validation failure — `code: "validation.failed"` + `errors` map

---

### GET /api/v1/boards/{boardId}

Get a board with all lanes and cards in order. Returns 404 for non-members (enumeration prevention).

- **Auth**: RegisteredUser
- **Rate limit**: authenticated
- **Path params**: `boardId: Guid`
- **Response 200**: `BoardDetailDto`
- **Response 404**: board not found OR caller is not a member — `code: "board.not_found"`

---

### DELETE /api/v1/boards/{boardId}

Delete a board and all its lanes, cards, and memberships atomically. Owner or Admin only.

- **Auth**: RegisteredUser + board Owner or Admin
- **Rate limit**: mutating
- **Path params**: `boardId: Guid`
- **Response 204**: success
- **Response 403**: caller lacks permission — `code: "board.forbidden"`
- **Response 404**: board not found or caller not a member — `code: "board.not_found"`

---

## Lanes

### POST /api/v1/boards/{boardId}/lanes

Add a lane to a board. Owner or Member role required.

- **Auth**: RegisteredUser + board Owner or Member
- **Rate limit**: mutating
- **Request body**: `CreateLaneRequest { name: string }`
- **Response 201**: `LaneDto`
- **Response 403**: caller is Viewer or not a member — `code: "lane.forbidden"`
- **Response 404**: board not found or caller not a member — `code: "board.not_found"`
- **Response 409**: lane name already exists in this board — `code: "lane.name_conflict"`
- **Response 422**: validation failure

---

### PATCH /api/v1/boards/{boardId}/lanes/{laneId}

Rename a lane. Owner or Member role required.

- **Auth**: RegisteredUser + board Owner or Member
- **Rate limit**: mutating
- **Request body**: `RenameLaneRequest { name: string }`
- **Response 200**: `LaneDto`
- **Response 403**: caller is Viewer — `code: "lane.forbidden"`
- **Response 404**: board or lane not found / caller not a member
- **Response 409**: name already taken in this board — `code: "lane.name_conflict"`
- **Response 422**: validation failure

---

### DELETE /api/v1/boards/{boardId}/lanes/{laneId}

Delete a lane and all its cards atomically.

- **Auth**: RegisteredUser + board Owner or Member
- **Rate limit**: mutating
- **Response 204**: success
- **Response 403**: caller is Viewer — `code: "lane.forbidden"`
- **Response 404**: board or lane not found / caller not a member

---

### POST /api/v1/boards/{boardId}/lanes/{laneId}/move

Move a lane to a new position within its board.

- **Auth**: RegisteredUser + board Owner or Member
- **Rate limit**: mutating
- **Request body**: `MoveLaneRequest { targetPosition: int, expectedVersion: int }`
- **Response 200**: `LaneDto` — with updated position and incremented version
- **Response 403**: caller is Viewer — `code: "lane.forbidden"`
- **Response 404**: board or lane not found / caller not a member
- **Response 409**: version mismatch (concurrent move) — `code: "lane.conflict"`
- **Response 422**: validation failure

---

## Cards

### POST /api/v1/boards/{boardId}/lanes/{laneId}/cards

Add a card to a lane.

- **Auth**: RegisteredUser + board Owner or Member
- **Rate limit**: mutating
- **Request body**: `CreateCardRequest { title: string, description?: string, dueDate?: string }`
- **Response 201**: `CardDto`
- **Response 403**: caller is Viewer — `code: "card.forbidden"`
- **Response 404**: board or lane not found / caller not a member
- **Response 422**: validation failure

---

### PATCH /api/v1/boards/{boardId}/cards/{cardId}

Update a card's title, description, or due date. All fields are optional; omit a field to
leave it unchanged. Pass `null` for `dueDate` to clear it.

- **Auth**: RegisteredUser + board Owner or Member
- **Rate limit**: mutating
- **Request body**: `UpdateCardRequest { title?: string, description?: string | null, dueDate?: string | null }`
- **Response 200**: `CardDto`
- **Response 403**: caller is Viewer — `code: "card.forbidden"`
- **Response 404**: card not found / caller not a member
- **Response 422**: validation failure

---

### DELETE /api/v1/boards/{boardId}/cards/{cardId}

Delete a card. Remaining cards in the lane have positions shifted down.

- **Auth**: RegisteredUser + board Owner or Member
- **Rate limit**: mutating
- **Response 204**: success
- **Response 403**: caller is Viewer — `code: "card.forbidden"`
- **Response 404**: card not found / caller not a member

---

### POST /api/v1/boards/{boardId}/cards/{cardId}/move

Move a card to a new position within its lane or to a different lane.

- **Auth**: RegisteredUser + board Owner or Member
- **Rate limit**: mutating
- **Request body**: `MoveCardRequest { targetLaneId: string, targetPosition: int, expectedVersion: int }`
- **Response 200**: `CardDto` — with updated position, laneId, and incremented version
- **Response 403**: caller is Viewer — `code: "card.forbidden"`
- **Response 404**: card or target lane not found / caller not a member
- **Response 409**: version mismatch (concurrent move) — `code: "card.conflict"`
- **Response 422**: validation failure

---

## Board Members

### GET /api/v1/boards/{boardId}/members

List all members of a board. Any board member may call this.

- **Auth**: RegisteredUser + any board member role
- **Rate limit**: authenticated
- **Response 200**: `BoardMemberDto[]` — ordered by `joinedAt` ascending
- **Response 404**: board not found / caller not a member

---

### POST /api/v1/boards/{boardId}/invites

Invite a person to join this board. Board Owner or Admin only. Reuses the invitation token
mechanism from 001-auth-onboarding; creates a board-scoped invitation record.

- **Auth**: RegisteredUser + board Owner or Admin
- **Rate limit**: mutating
- **Request body**: `InviteBoardMemberRequest { email: string, role: "Owner" | "Member" | "Viewer" }`
- **Response 201**: `IssueInviteResponse` (same DTO as system-level invite) — new invitation
- **Response 200**: `IssueInviteResponse` — returned existing unconsumed invitation for same email + board
- **Response 403**: caller is Member or Viewer — `code: "member.forbidden"`
- **Response 404**: board not found / caller not a member
- **Response 409**: invitee is already a member of this board — `code: "member.already_member"`
- **Response 422**: validation failure

---

### PATCH /api/v1/boards/{boardId}/members/{userId}

Change a member's role. Board Owner or Admin only.

- **Auth**: RegisteredUser + board Owner or Admin
- **Rate limit**: mutating
- **Request body**: `ChangeMemberRoleRequest { role: "Owner" | "Member" | "Viewer" }`
- **Response 200**: `BoardMemberDto`
- **Response 400**: cannot change last Owner's role — `code: "member.last_owner"`
- **Response 403**: caller lacks permission — `code: "member.forbidden"`
- **Response 404**: board or member not found / caller not a member
- **Response 422**: validation failure

---

### DELETE /api/v1/boards/{boardId}/members/{userId}

Remove a member from a board.

- **Auth**: RegisteredUser + board Owner or Admin
- **Rate limit**: mutating
- **Response 204**: success
- **Response 400**: cannot remove last Owner — `code: "member.last_owner"`
- **Response 403**: caller lacks permission — `code: "member.forbidden"`
- **Response 404**: board or member not found / caller not a member

---

## Extended Endpoint (Modified from 001)

### POST /api/v1/invites/{token}/accept *(extended)*

Unchanged URL and behavior from 001, extended to also activate board membership when the
invitation is board-scoped (invitation.board_id is non-null).

- On success: creates User (if new) AND creates BoardMember record (if invitation has board_id)
- Response 200 now returns `AcceptInviteResponseDto { user: CurrentUserDto, boardId?: string }` so the frontend can redirect to the invited board
- All other behaviors (401, 410, 422) unchanged from 001
