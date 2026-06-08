import { test, expect } from '@playwright/test'
import { ADMIN_AUTH, UNREGISTERED_AUTH, API_BASE } from '../auth.helpers'

test.describe('US1 scenario 1+2 — admin signs in and is recognized', () => {
  test.use({ storageState: ADMIN_AUTH })

  test('scenario 1 — admin OAuth completes and app is accessible', async ({ page }) => {
    await page.goto('/')
    await expect(page).not.toHaveURL(/\/signin/)
    await expect(page.getByRole('heading', { level: 1, name: /boards/i })).toBeVisible()
  })

  test('scenario 2 — subsequent sign-in is recognized; /auth/me returns admin role', async ({
    page,
  }) => {
    await page.goto('/')
    await expect(page).not.toHaveURL(/\/signin/)

    const resp = await page.request.get(`${API_BASE}/api/v1/auth/me`)
    expect(resp.status()).toBe(200)
    const body = await resp.json()
    expect(body.systemRole).toBe('admin')
  })
})

test.describe('US1 scenario 3 — non-registered Google user sees not-registered page', () => {
  test.use({ storageState: UNREGISTERED_AUTH })

  test('scenario 3 — non-invited account is redirected to /not-registered with no app data', async ({
    page,
  }) => {
    await page.goto('/')
    await expect(page).toHaveURL(/\/not-registered/)
    await expect(
      page.getByRole('heading', { name: /not registered|access denied/i }),
    ).toBeVisible()
    await expect(page.getByRole('navigation')).not.toBeVisible()
  })
})

test.describe('US1 scenario 4 — unauthenticated access redirects to sign-in', () => {
  test('scenario 4 — unauthenticated root navigates to /signin', async ({ browser }) => {
    const ctx = await browser.newContext()
    const page = await ctx.newPage()
    await page.goto('/')
    await expect(page).toHaveURL(/\/signin/)
    await expect(page.getByRole('heading', { name: /sign in/i })).toBeVisible()
    await expect(page.getByRole('link', { name: /sign in with google/i })).toBeVisible()
    await ctx.close()
  })
})
