INSERT INTO users (id, email, display_name, system_role, google_sub, registered_at)
VALUES (
    '$AdminUserId$',
    '$AdminEmail$',
    'Administrator',
    'Admin',
    NULL,
    '$SeedTimestamp$'
)
ON CONFLICT DO NOTHING;
