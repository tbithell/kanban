CREATE TABLE IF NOT EXISTS users (
    id              TEXT NOT NULL PRIMARY KEY,
    email           TEXT NOT NULL,
    display_name    TEXT NOT NULL,
    system_role     TEXT NOT NULL DEFAULT 'Standard',
    google_sub      TEXT,
    registered_at   TEXT NOT NULL,
    last_sign_in_at TEXT,
    CONSTRAINT uq_users_email      UNIQUE (email),
    CONSTRAINT uq_users_google_sub UNIQUE (google_sub)
);

CREATE TABLE IF NOT EXISTS invitations (
    id                   TEXT NOT NULL PRIMARY KEY,
    email                TEXT NOT NULL,
    issued_by_user_id    TEXT NOT NULL REFERENCES users(id),
    token_hash           TEXT NOT NULL,
    issued_at            TEXT NOT NULL,
    expires_at           TEXT NOT NULL,
    consumed_at          TEXT,
    consumed_by_user_id  TEXT REFERENCES users(id),
    CONSTRAINT uq_invitations_token_hash UNIQUE (token_hash)
);

CREATE INDEX IF NOT EXISTS ix_invitations_email      ON invitations(email);
CREATE INDEX IF NOT EXISTS ix_invitations_token_hash ON invitations(token_hash);

CREATE TABLE IF NOT EXISTS auth_events (
    id           TEXT NOT NULL PRIMARY KEY,
    occurred_at  TEXT NOT NULL,
    event_type   TEXT NOT NULL,
    user_id      TEXT REFERENCES users(id),
    outcome      TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_auth_events_occurred_at ON auth_events(occurred_at);
