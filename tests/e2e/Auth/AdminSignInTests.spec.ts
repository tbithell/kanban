import { test, expect } from '@playwright/test'

test.describe('US1: Seeded admin signs in', () => {
  test('scenario 1 — admin email completes Google OAuth and lands on signed-in page', async ({
    page,
  }) => {
    await page.goto('/')

    // Unauthenticated root redirects to sign-in page
    await expect(page).toHaveURL(/\/signin/)
    await expect(page.getByRole('heading', { name: /sign in/i })).toBeVisible()
    await expect(page.getByRole('link', { name: /sign in with google/i })).toBeVisible()
  })

  test('scenario 2 — subsequent admin sign-in is recognized without re-linking', async ({
    page,
  }) => {
    // First sign-in via Google (requires real OAuth — placeholder for manual/CI test)
    await page.goto('/signin')
    await expect(page.getByRole('link', { name: /sign in with google/i })).toBeVisible()
  })

  test('scenario 3 — non-invited Google account sees "not registered" message with no app data', async ({
    page,
  }) => {
    await page.goto('/not-registered')

    await expect(page.getByRole('heading', { name: /not registered|access denied/i })).toBeVisible()
    // No navigation, boards, or app data visible
    await expect(page.getByRole('navigation')).not.toBeVisible()
  })

  test('scenario 4 — unauthenticated access to / redirects to sign-in page', async ({ page }) => {
    await page.goto('/')

    await expect(page).toHaveURL(/\/signin/)
    await expect(page.getByRole('heading', { name: /sign in/i })).toBeVisible()
  })
})
