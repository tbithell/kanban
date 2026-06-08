import { test, expect } from '@playwright/test'
import {
  ADMIN_AUTH, INVITEE_AUTH,
  API_BASE, WEB_BASE,
  INVITEE_EMAIL,
} from '../auth.helpers'

// ── US2: Admin issues an invitation ──────────────────────────────────────────
// These scenarios require the board page (InviteUserDialog lives there).
// Scaffolded now; will be completed in the board feature spec.

test.describe('US2: Admin issues an invitation', () => {
  test.use({ storageState: ADMIN_AUTH })

  test('scenario 1 — admin submits valid email and receives redemption link', async ({ page }) => {
    test.fixme(true, 'Requires board page — InviteUserDialog not yet accessible')
    await page.goto('/')
    await expect(page.getByRole('main', { name: /kanban board/i })).toBeVisible()
  })

  test('scenario 2 — re-invite of unconsumed email returns existing link without duplicate', async ({ page }) => {
    test.fixme(true, 'Requires board page')
    await page.goto('/')
  })

  test('scenario 3 — re-invite of expired email issues fresh invitation', async ({ page }) => {
    test.fixme(true, 'Requires board page')
    await page.goto('/')
  })

  test('scenario 4 — non-admin user is refused with permission denied', async ({ page }) => {
    test.fixme(true, 'Requires board page')
    await page.goto('/')
  })

  test('scenario 5 — invalid email format is rejected with validation error', async ({ page }) => {
    test.fixme(true, 'Requires board page')
    await page.goto('/')
  })
})

// ── US3: Invitee accepts an invitation ───────────────────────────────────────

test.describe('US3: Invitee accepts an invitation', () => {
  test('scenario 1 — valid token with matching email creates registered user', async ({
    browser,
    request,
  }) => {
    const uniqueEmail = `invitee-${Date.now()}@example.com`

    const seedResp = await request.post(`${API_BASE}/api/v1/dev/seed/invitation`, {
      data: { email: uniqueEmail },
    })
    expect(seedResp.ok()).toBeTruthy()
    const { token } = await seedResp.json()

    const ctx = await browser.newContext()
    const page = await ctx.newPage()
    await page.goto(
      `${API_BASE}/api/v1/dev/authenticate?email=${encodeURIComponent(uniqueEmail)}&displayName=Scenario+1+Invitee`,
    )
    await page.waitForURL(`${WEB_BASE}/**`)

    await page.goto(`${WEB_BASE}/accept/${token}`)
    await expect(page).toHaveURL(`${WEB_BASE}/`, { timeout: 15_000 })
    await expect(page.getByRole('heading', { level: 1, name: /boards/i })).toBeVisible()

    await ctx.close()
  })

  test('scenario 2 — invitee can sign in via regular flow after accepting', async ({ page }) => {
    test.fixme(true, 'Depends on scenario 1 having registered the invitee; requires board page for full verification')
    await page.goto('/')
  })

  test.describe('error states — use invitee auth cookie', () => {
    test.use({ storageState: INVITEE_AUTH })

    test('scenario 3 — expired token shows invitation-no-longer-valid error', async ({
      page,
      request,
    }) => {
      const seedResp = await request.post(`${API_BASE}/api/v1/dev/seed/invitation`, {
        data: { email: INVITEE_EMAIL, expiresInDays: -1 },
      })
      const { token } = await seedResp.json()

      await page.goto(`${WEB_BASE}/accept/${token}`)
      await expect(
        page.getByRole('alert').filter({ hasText: /no longer valid|invite\.invalid/i }),
      ).toBeVisible({ timeout: 10_000 })
    })

    test('scenario 4 — consumed token shows same invalid message as expired', async ({
      page,
      request,
    }) => {
      const seedResp = await request.post(`${API_BASE}/api/v1/dev/seed/invitation`, {
        data: { email: INVITEE_EMAIL, consumed: true },
      })
      const { token } = await seedResp.json()

      await page.goto(`${WEB_BASE}/accept/${token}`)
      await expect(
        page.getByRole('alert').filter({ hasText: /no longer valid|invite\.invalid/i }),
      ).toBeVisible({ timeout: 10_000 })
    })

    test('scenario 5 — email mismatch shows different email address message', async ({
      page,
      request,
    }) => {
      // Invitation is for a different email; cookie is for INVITEE_EMAIL
      const seedResp = await request.post(`${API_BASE}/api/v1/dev/seed/invitation`, {
        data: { email: 'different-addressee@example.com' },
      })
      const { token } = await seedResp.json()

      await page.goto(`${WEB_BASE}/accept/${token}`)
      await expect(
        page.getByRole('alert').filter({ hasText: /different email|invite\.email_mismatch/i }),
      ).toBeVisible({ timeout: 10_000 })
    })

    test('scenario 6 — fabricated token shows same invalid message', async ({ page }) => {
      const fakeToken = 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'
      await page.goto(`${WEB_BASE}/accept/${fakeToken}`)
      await expect(
        page.getByRole('alert').filter({ hasText: /no longer valid|invite\.invalid/i }),
      ).toBeVisible({ timeout: 10_000 })
    })
  })
})
