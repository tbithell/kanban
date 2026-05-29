export const ADMIN_AUTH = 'playwright/.auth/admin.json'
export const INVITEE_AUTH = 'playwright/.auth/invitee.json'
export const UNREGISTERED_AUTH = 'playwright/.auth/unregistered.json'

export const API_BASE = process.env.API_BASE_URL ?? 'http://localhost:5077'
export const WEB_BASE = process.env.WEB_BASE_URL ?? 'http://localhost:5173'

export const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? 'admin@test.local'
export const INVITEE_EMAIL = 'playwright-invitee@example.com'
export const UNREGISTERED_EMAIL = 'playwright-unregistered@example.com'
