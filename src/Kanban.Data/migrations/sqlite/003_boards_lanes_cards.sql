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
