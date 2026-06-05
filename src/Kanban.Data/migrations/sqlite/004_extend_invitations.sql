ALTER TABLE invitations ADD COLUMN board_id   TEXT REFERENCES boards(id);
ALTER TABLE invitations ADD COLUMN board_role  TEXT;
