# Quickstart: Kanban Core

**Branch**: `002-kanban-core`  
**Purpose**: Get the core Kanban board features running locally, building on the completed 001-auth-onboarding foundation.

---

## Prerequisites

The 001-auth-onboarding feature must be fully working before starting this feature:

- [ ] 001 verified onboarding checklist complete (admin can sign in, invitations work)
- [ ] All 001 unit + integration + component + e2e tests pass
- [ ] `dotnet user-secrets list` shows all four secrets (Google, DB, Seed)
- [ ] dnd-kit packages available: `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities`

---

## Install Additional Frontend Packages

```bash
cd src/Kanban.Web
npm install @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities
```

---

## Run Migrations

The 002 migrations add `boards`, `board_members`, `lanes`, `cards`, and `card_assignees` tables,
and extend the existing `invitations` table with `board_id` and `board_role` columns.

```bash
# Migrations run automatically on API startup via DbUp
cd src/Kanban.Api
dotnet run
# Look for: "Applied 2 script(s)" in output (scripts 003 and 004)
```

---

## Port Map (unchanged from 001)

| Service | URL |
|---------|-----|
| API (HTTP) | http://localhost:5077 |
| API (HTTPS) | https://localhost:7282 |
| Vite dev server | http://localhost:5173 |
| Scalar API docs | http://localhost:5077/scalar/v1 |

---

## First Board Walkthrough

1. Sign in as admin at http://localhost:5173
2. Click "Create Board" (admin-only action), enter a name, submit
3. Confirm the board appears in the board list
4. Open the board, click "Add Lane", create "To Do", "In Progress", "Done"
5. Click "+ Add Card" in "To Do", create a card with a title
6. Drag the card to "In Progress" — confirm the move is reflected immediately and persists on refresh
7. Use keyboard: Tab to focus a card, Space to pick it up, arrow keys to move, Space to drop

---

## Invite a Member to a Board

1. Open the board as admin (the board's Owner)
2. Open the Members panel → "Invite Member"
3. Enter an email and select the "Member" role → submit
4. Copy the redemption link
5. Open the link in a new incognito window; complete Google sign-in with the invited email
6. Confirm the invitee lands on the board they were invited to with Member access
7. Verify: invitee can add cards but cannot invite members

---

## Run Tests

```bash
# Unit tests (includes new Board, Lane, Card, BoardMembership service tests)
dotnet test tests/unit/

# Integration tests (real SQLite, includes new endpoint tests)
dotnet test tests/integration/

# Frontend component tests (includes KanbanBoard, Lane, CardItem tests)
cd src/Kanban.Web && npm test

# E2E tests (API must be running; includes drag-drop and membership tests)
cd tests/e2e && npm run test:e2e
```

---

## Verified Feature Checklist

- [ ] Admin can create a board
- [ ] Three lanes can be added to a board and appear in order
- [ ] Cards can be created in each lane
- [ ] Card drag between lanes persists on reload
- [ ] Lane reorder persists on reload
- [ ] Keyboard drag completes successfully (Space to pick up, arrow to move, Space to drop)
- [ ] A board Viewer cannot add or move cards (gets permission-denied)
- [ ] A non-member gets 404 for any board resource request
- [ ] Board invitation with Member role: invitee lands on board after acceptance
- [ ] Concurrent move test: call move endpoint twice in parallel for same card; one gets 409
- [ ] All four test layers pass
