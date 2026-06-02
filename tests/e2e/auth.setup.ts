import { test as setup, expect } from '@playwright/test'
import fs from 'fs'
import {
  ADMIN_AUTH, INVITEE_AUTH, UNREGISTERED_AUTH,
  API_BASE, WEB_BASE,
  ADMIN_EMAIL, INVITEE_EMAIL, UNREGISTERED_EMAIL,
} from './auth.helpers'

const BYPASS = process.env.PLAYWRIGHT_AUTH_BYPASS === 'true'

setup.beforeAll(() => {
  fs.mkdirSync('playwright/.auth', { recursive: true })
  const emptyState = JSON.stringify({ cookies: [], origins: [] })
  for (const p of [INVITEE_AUTH, UNREGISTERED_AUTH]) {
    if (!fs.existsSync(p)) fs.writeFileSync(p, emptyState)
  }
})

// ── CI / bypass path ──────────────────────────────────────────────────────────
// Calls GET /api/v1/dev/authenticate which issues the cookie without Google OAuth
// and redirects to the frontend. Only registered when ASPNETCORE_ENVIRONMENT=Development.

setup('bypass: authenticate as admin', async ({ page }) => {
  setup.skip(!BYPASS, 'bypass path only — run with PLAYWRIGHT_AUTH_BYPASS=true')
  await page.goto(`${API_BASE}/api/v1/dev/authenticate?email=${encodeURIComponent(ADMIN_EMAIL)}`)
  await page.waitForURL(`${WEB_BASE}/**`)
  await page.context().storageState({ path: ADMIN_AUTH })
})

setup('authenticate as unregistered invitee', async ({ page }) => {
  // No Google sign-in path exists for test invitees — dev endpoint is the only option
  // regardless of whether PLAYWRIGHT_AUTH_BYPASS is set.
  await page.goto(
    `${API_BASE}/api/v1/dev/authenticate` +
    `?email=${encodeURIComponent(INVITEE_EMAIL)}&displayName=Playwright+Invitee`,
  )
  await page.waitForURL(`${WEB_BASE}/**`)
  await page.context().storageState({ path: INVITEE_AUTH })
})

setup('authenticate as unregistered non-invited user', async ({ page }) => {
  // No Google sign-in path exists for test non-invited users — dev endpoint is the only option.
  await page.goto(
    `${API_BASE}/api/v1/dev/authenticate` +
    `?email=${encodeURIComponent(UNREGISTERED_EMAIL)}&displayName=Playwright+Unregistered`,
  )
  await page.waitForURL(`${WEB_BASE}/**`)
  await page.context().storageState({ path: UNREGISTERED_AUTH })
})

// ── Demo / real Google path ───────────────────────────────────────────────────
// Opens a headed browser window and waits (up to 2 minutes) for you to complete
// Google sign-in manually. Run once with: npm run test:e2e:setup
// The saved state is reused for all subsequent demo runs.

setup('google: authenticate as admin', async ({ page, context }) => {
  setup.skip(BYPASS, 'real Google path only — run without PLAYWRIGHT_AUTH_BYPASS=true')
  setup.skip(fs.existsSync(ADMIN_AUTH), 'saved session exists — delete playwright/.auth/admin.json to force re-auth')
  setup.setTimeout(180_000)
  await page.goto(`${WEB_BASE}/signin`)
  await expect(page.getByRole('link', { name: /sign in with google/i })).toBeVisible()
  await page.click('a[href*="/api/v1/auth/signin"]')
  // Wait up to 3 minutes for the user to complete Google sign-in in the browser window
  await page.waitForURL(`${WEB_BASE}/**`, { timeout: 170_000 })
  await expect(page).not.toHaveURL(/\/signin/)
  await context.storageState({ path: ADMIN_AUTH })
})
