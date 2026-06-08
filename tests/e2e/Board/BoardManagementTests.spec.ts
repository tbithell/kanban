import { test, expect } from '@playwright/test'
import { ADMIN_AUTH, API_BASE, WEB_BASE } from '../auth.helpers'

// ── US1: Admin creates a board and adds lanes ─────────────────────────────────

test.describe('US1: Admin creates a board and adds lanes', () => {
  test.use({ storageState: ADMIN_AUTH })

  test('scenario 1 — admin creates a board and is assigned Owner role', async ({ page }) => {
    await page.goto(`${WEB_BASE}/`)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    await page.getByRole('button', { name: /create board/i }).click()
    const nameInput = page.getByRole('textbox', { name: /board name/i })
    await nameInput.fill('Scenario 1 Board')
    await page.getByRole('button', { name: /^create$/i }).click()

    await expect(page).toHaveURL(/\/boards\/[0-9a-f-]+/, { timeout: 10_000 })
    await expect(page.getByRole('heading', { name: 'Scenario 1 Board' })).toBeVisible()
  })

  test('scenario 2 — admin adds three lanes and they appear in insertion order', async ({
    page,
    request,
  }) => {
    const boardName = `Lane Order Board ${Date.now()}`
    const createResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: boardName },
    })
    expect(createResp.ok()).toBeTruthy()
    const board = await createResp.json()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByRole('heading', { name: boardName })).toBeVisible()

    for (const laneName of ['To Do', 'In Progress', 'Done']) {
      await page.getByRole('button', { name: /add lane/i }).click()
      await page.getByRole('textbox', { name: /lane name/i }).fill(laneName)
      await page.getByRole('button', { name: /^add$/i }).click()
      await expect(page.getByRole('heading', { name: laneName })).toBeVisible()
    }

    const lanes = page.getByRole('region', { name: /lane/i })
    await expect(lanes.nth(0)).toContainText('To Do')
    await expect(lanes.nth(1)).toContainText('In Progress')
    await expect(lanes.nth(2)).toContainText('Done')
  })

  test('scenario 3 — board list shows the newly created board', async ({ page, request }) => {
    const boardName = `Listed Board ${Date.now()}`
    await request.post(`${API_BASE}/api/v1/boards`, { data: { name: boardName } })

    await page.goto(`${WEB_BASE}/`)
    await expect(page.getByText(boardName)).toBeVisible({ timeout: 10_000 })
  })

  test('scenario 4 — duplicate board name is rejected with conflict error', async ({
    page,
    request,
  }) => {
    const boardName = `Duplicate Board ${Date.now()}`
    await request.post(`${API_BASE}/api/v1/boards`, { data: { name: boardName } })

    await page.goto(`${WEB_BASE}/`)
    await page.getByRole('button', { name: /create board/i }).click()
    await page.getByRole('textbox', { name: /board name/i }).fill(boardName)
    await page.getByRole('button', { name: /^create$/i }).click()

    await expect(page.getByRole('alert')).toBeVisible({ timeout: 10_000 })
  })

  test('scenario 5 — duplicate lane name within a board is rejected', async ({
    page,
    request,
  }) => {
    const boardName = `Dup Lane Board ${Date.now()}`
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: boardName },
    })
    const board = await boardResp.json()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await page.getByRole('button', { name: /add lane/i }).click()
    await page.getByRole('textbox', { name: /lane name/i }).fill('Backlog')
    await page.getByRole('button', { name: /^add$/i }).click()
    await expect(page.getByRole('heading', { name: 'Backlog' })).toBeVisible()

    await page.getByRole('button', { name: /add lane/i }).click()
    await page.getByRole('textbox', { name: /lane name/i }).fill('Backlog')
    await page.getByRole('button', { name: /^add$/i }).click()

    await expect(page.getByRole('alert')).toBeVisible({ timeout: 10_000 })
  })
})

// ── US1: Non-admin cannot create boards ──────────────────────────────────────

test.describe('US1: Non-admin user cannot create boards', () => {
  test('scenario 6 — non-admin sees no create board button', async ({ browser, request }) => {
    const uniqueEmail = `member-${Date.now()}@example.com`
    const ctx = await browser.newContext()
    const page = await ctx.newPage()

    await page.goto(
      `${API_BASE}/api/v1/dev/authenticate?email=${encodeURIComponent(uniqueEmail)}&displayName=Member+User`,
    )
    await page.waitForURL(`${WEB_BASE}/**`)

    await page.goto(`${WEB_BASE}/`)

    await expect(page.getByRole('button', { name: /create board/i })).not.toBeVisible()

    await ctx.close()
  })
})
