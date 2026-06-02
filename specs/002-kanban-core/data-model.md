# Data Model: Kanban Core

**Phase**: 1 | **Branch**: `002-kanban-core`

---

## Domain Entities

### Board (Aggregate Root)

Represents a named workspace. Only system administrators can create boards. Creating a board
automatically grants the creator the Owner board role.

```csharp
public sealed class Board
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAt { get; }

    public Board(Guid id, string name, Guid createdByUserId, DateTimeOffset createdAt)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(name).IsNotNull().IsNotEmpty().HasMaxLength(200);
        Verify.That(createdByUserId).IsNotDefault();
        Id = id;
        Name = name;
        CreatedByUserId = createdByUserId;
        CreatedAt = createdAt;
    }

    public void Rename(string name)
    {
        Verify.That(name).IsNotNull().IsNotEmpty().HasMaxLength(200);
        Name = name;
    }
}
```

### Lane (Aggregate Root)

An ordered column within a Board. Names are unique within a board. Positions are gapless
integers; reordering shifts sibling positions atomically.

```csharp
public sealed class Lane
{
    public Guid Id { get; }
    public Guid BoardId { get; }
    public string Name { get; private set; }
    public int Position { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public Lane(Guid id, Guid boardId, string name, int position, int version,
                DateTimeOffset createdAt)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(boardId).IsNotDefault();
        Verify.That(name).IsNotNull().IsNotEmpty().HasMaxLength(100);
        Verify.That(position).IsPositive<int>();
        Id = id;
        BoardId = boardId;
        Name = name;
        Position = position;
        Version = version;
        CreatedAt = createdAt;
    }

    public void Rename(string name)
    {
        Verify.That(name).IsNotNull().IsNotEmpty().HasMaxLength(100);
        Name = name;
    }

    public void MoveTo(int position)
    {
        Verify.That(position).IsPositive<int>();
        Position = position;
        Version++;
    }
}
```

### Card (Aggregate Root)

A work item within a Lane. Titles are required; description and due date are optional.
Cards can be moved between lanes. `board_id` is denormalized for membership checks without
requiring a JOIN through lanes.

```csharp
public sealed class Card
{
    public Guid Id { get; }
    public Guid LaneId { get; private set; }
    public Guid BoardId { get; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public int Position { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Card(Guid id, Guid laneId, Guid boardId, string title, string? description,
                DateOnly? dueDate, int position, int version,
                DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(laneId).IsNotDefault();
        Verify.That(boardId).IsNotDefault();
        Verify.That(title).IsNotNull().IsNotEmpty().HasMaxLength(200);
        if (description is not null) Verify.That(description).HasMaxLength(2000);
        Verify.That(position).IsPositive<int>();
        // assign all fields
    }

    public void Update(string title, string? description, DateOnly? dueDate,
                       DateTimeOffset updatedAt)
    {
        Verify.That(title).IsNotNull().IsNotEmpty().HasMaxLength(200);
        if (description is not null) Verify.That(description).HasMaxLength(2000);
        Title = title;
        Description = description;
        DueDate = dueDate;
        UpdatedAt = updatedAt;
    }

    public void MoveTo(Guid laneId, int position, DateTimeOffset updatedAt)
    {
        Verify.That(laneId).IsNotDefault();
        Verify.That(position).IsPositive<int>();
        LaneId = laneId;
        Position = position;
        Version++;
        UpdatedAt = updatedAt;
    }
}
```

### BoardMember

Association between a registered User and a Board with an assigned role. Not an aggregate
root — accessed through Board or User context.

```csharp
public sealed class BoardMember
{
    public Guid Id { get; }
    public Guid BoardId { get; }
    public Guid UserId { get; }
    public BoardRole Role { get; private set; }
    public Guid? InvitedByUserId { get; }
    public DateTimeOffset JoinedAt { get; }

    public BoardMember(Guid id, Guid boardId, Guid userId, BoardRole role,
                       Guid? invitedByUserId, DateTimeOffset joinedAt)
    {
        Verify.That(id).IsNotDefault();
        Verify.That(boardId).IsNotDefault();
        Verify.That(userId).IsNotDefault();
        // assign all fields
    }

    public void ChangeRole(BoardRole newRole) => Role = newRole;
}
```

### Enumerations

```csharp
// Kanban.Domain/Enums/BoardRole.cs
public enum BoardRole { Owner, Member, Viewer }
```

---

## Database Schema

### Migration 003 — SQLite (sqlite/003_boards_lanes_cards.sql)

```sql
CREATE TABLE IF NOT EXISTS boards (
    id                  TEXT NOT NULL PRIMARY KEY,
    name                TEXT NOT NULL,
    created_by_user_id  TEXT NOT NULL REFERENCES users(id),
    created_at          TEXT NOT NULL,
    CONSTRAINT uq_boards_name UNIQUE (name)
);

CREATE TABLE IF NOT EXISTS board_members (
    id                  TEXT NOT NULL PRIMARY KEY,
    board_id            TEXT NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    user_id             TEXT NOT NULL REFERENCES users(id),
    role                TEXT NOT NULL,
    invited_by_user_id  TEXT REFERENCES users(id),
    joined_at           TEXT NOT NULL,
    CONSTRAINT uq_board_members_board_user UNIQUE (board_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_board_members_user_id ON board_members(user_id);

CREATE TABLE IF NOT EXISTS lanes (
    id          TEXT NOT NULL PRIMARY KEY,
    board_id    TEXT NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    name        TEXT NOT NULL,
    position    INTEGER NOT NULL,
    version     INTEGER NOT NULL DEFAULT 1,
    created_at  TEXT NOT NULL,
    CONSTRAINT uq_lanes_board_name UNIQUE (board_id, name),
    CONSTRAINT uq_lanes_board_position UNIQUE (board_id, position)
);

CREATE INDEX IF NOT EXISTS ix_lanes_board_id ON lanes(board_id);

CREATE TABLE IF NOT EXISTS cards (
    id           TEXT NOT NULL PRIMARY KEY,
    lane_id      TEXT NOT NULL REFERENCES lanes(id) ON DELETE CASCADE,
    board_id     TEXT NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    title        TEXT NOT NULL,
    description  TEXT,
    due_date     TEXT,
    position     INTEGER NOT NULL,
    version      INTEGER NOT NULL DEFAULT 1,
    created_at   TEXT NOT NULL,
    updated_at   TEXT NOT NULL,
    CONSTRAINT uq_cards_lane_position UNIQUE (lane_id, position)
);

CREATE INDEX IF NOT EXISTS ix_cards_lane_id  ON cards(lane_id);
CREATE INDEX IF NOT EXISTS ix_cards_board_id ON cards(board_id);

CREATE TABLE IF NOT EXISTS card_assignees (
    id           TEXT NOT NULL PRIMARY KEY,
    card_id      TEXT NOT NULL REFERENCES cards(id) ON DELETE CASCADE,
    user_id      TEXT NOT NULL REFERENCES users(id),
    assigned_at  TEXT NOT NULL,
    CONSTRAINT uq_card_assignees_card_user UNIQUE (card_id, user_id)
);
```

### Migration 003 — Postgres (postgres/003_boards_lanes_cards.sql)

Identical to the SQLite version — all syntax used (TEXT, INTEGER, REFERENCES, ON DELETE
CASCADE, CREATE INDEX IF NOT EXISTS) is ANSI-compatible.

### Migration 004 — SQLite + Postgres (004_extend_invitations.sql)

Adds board context to the existing `invitations` table (supports board-scoped invitations
from FR-015–FR-018).

```sql
ALTER TABLE invitations ADD COLUMN board_id   TEXT REFERENCES boards(id);
ALTER TABLE invitations ADD COLUMN board_role  TEXT;
```

Both columns are nullable. Null `board_id` = system-level invitation (existing 001 behavior).
Non-null `board_id` = board-scoped invitation.

---

## Relationships

```
boards (1) ──< board_members >── (N) users
boards (1) ──< lanes (N)
boards (1) ──< cards (N)      [denormalized]
lanes  (1) ──< cards (N)
cards  (1) ──< card_assignees >── (N) users
invitations (optional) ──> boards
invitations (optional) sets board_role on acceptance
```

---

## Position Invariants

**Lane position**: unique per `board_id`; must always form the sequence 1…N after every
create, reorder, and delete.

**Card position**: unique per `lane_id`; must always form the sequence 1…N after every
create, move, and delete.

**Reorder algorithm** (example: move item from oldPos to newPos, oldPos > newPos):

```sql
-- Step 1: shift up items in the gap
UPDATE lanes
SET position = position + 1
WHERE board_id = @boardId
  AND position >= @newPos
  AND position < @oldPos;

-- Step 2: place the moved item
UPDATE lanes
SET position = @newPos, version = version + 1
WHERE id = @laneId
  AND version = @expectedVersion;  -- optimistic lock
```

If step 2 returns 0 rows, the caller returns 409 Conflict.

---

## Value Objects (None New)

`InvitationToken` from 001 is reused for board invitations — the same 256-bit random
token with SHA-256 hash. No new value objects are required for this feature.
