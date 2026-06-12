import { test, expect } from '@playwright/test'
import { ADMIN_AUTH, API_BASE, WEB_BASE } from '../auth.helpers'

// ── US1: Admin creates a board and adds lanes ─────────────────────────────────

test.describe('US1: Admin creates a board and adds lanes', () => {
  test.use({ storageState: ADMIN_AUTH })

  test.skip('scenario 1 — admin creates a board and is assigned Owner role', async ({ page }) => {
    const boardName = `Scenario 1 Board ${Date.now()}`

    await page.goto(`${WEB_BASE}/`)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()

    await page.getByRole('button', { name: /create board/i }).click()
    const nameInput = page.getByRole('textbox', { name: /board name/i })
    await nameInput.fill(boardName)
    await page.getByRole('button', { name: /^create$/i }).click()

    await expect(page).toHaveURL(/\/boards\/[0-9a-f-]+/, { timeout: 10_000 })
    await expect(page.getByRole('heading', { level: 1 })).toContainText(boardName, { timeout: 15_000 })
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
    await expect(page.getByRole('heading', { name: boardName })).toBeVisible({ timeout: 10_000 })

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
    await expect(page.getByRole('heading', { name: boardName })).toBeVisible({ timeout: 10_000 })
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
  test('scenario 6 — non-admin sees no create board button', async ({ browser }) => {
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

// ── US5: Viewer browses a board read-only ────────────────────────────────────

test.describe('US5: Viewer browses a board read-only', () => {
  test.use({ storageState: ADMIN_AUTH })

  test('scenario 1 — viewer sees all lanes and cards but no write controls', async ({
    page,
    request,
    browser,
  }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Viewer Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()

    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Backlog' },
    })
    expect(laneResp.ok()).toBeTruthy()
    const lane = await laneResp.json()

    const cardResp = await request.post(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/cards`,
      { data: { title: 'Sample Card' } },
    )
    expect(cardResp.ok()).toBeTruthy()

    const viewerEmail = `e2e-us5-viewer-${Date.now()}@example.com`
    const seedResp = await request.post(`${API_BASE}/api/v1/dev/seed-board-member`, {
      data: { email: viewerEmail, boardId: board.id, role: 'Viewer' },
    })
    expect(seedResp.ok(), `Seed viewer failed: ${seedResp.status()}`).toBeTruthy()

    const viewerCtx = await browser.newContext()
    const viewerPage = await viewerCtx.newPage()
    await viewerPage.goto(
      `${API_BASE}/api/v1/dev/authenticate?email=${encodeURIComponent(viewerEmail)}&displayName=Viewer+User`,
    )
    await viewerPage.waitForURL(`${WEB_BASE}/**`)

    await viewerPage.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(viewerPage.getByRole('heading', { name: board.name })).toBeVisible({
      timeout: 10_000,
    })

    // Lane and card content is visible
    await expect(viewerPage.getByRole('region', { name: /lane: backlog/i })).toBeVisible()
    await expect(viewerPage.getByText('Sample Card')).toBeVisible()

    // Write controls are hidden
    await expect(viewerPage.getByRole('button', { name: /add lane/i })).not.toBeVisible()
    await expect(viewerPage.getByRole('button', { name: /add card/i })).not.toBeVisible()
    await expect(viewerPage.getByRole('button', { name: /edit card/i })).not.toBeVisible()
    await expect(viewerPage.getByRole('button', { name: /delete card/i })).not.toBeVisible()
    await expect(viewerPage.getByRole('button', { name: /delete lane/i })).not.toBeVisible()
    await expect(viewerPage.getByRole('button', { name: /drag to reorder/i })).not.toBeVisible()

    await viewerCtx.close()
  })

  test('scenario 2 — viewer direct API write attempts all return 403', async ({
    request,
    browser,
  }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Viewer API Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()

    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'To Do' },
    })
    expect(laneResp.ok()).toBeTruthy()
    const lane = await laneResp.json()

    const cardResp = await request.post(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/cards`,
      { data: { title: 'Existing Card' } },
    )
    expect(cardResp.ok()).toBeTruthy()
    const card = await cardResp.json()

    const viewerEmail = `e2e-us5-api-viewer-${Date.now()}@example.com`
    const seedResp = await request.post(`${API_BASE}/api/v1/dev/seed-board-member`, {
      data: { email: viewerEmail, boardId: board.id, role: 'Viewer' },
    })
    expect(seedResp.ok(), `Seed viewer failed: ${seedResp.status()}`).toBeTruthy()

    const viewerCtx = await browser.newContext()
    const viewerPage = await viewerCtx.newPage()
    await viewerPage.goto(
      `${API_BASE}/api/v1/dev/authenticate?email=${encodeURIComponent(viewerEmail)}&displayName=Viewer+User`,
    )
    await viewerPage.waitForURL(`${WEB_BASE}/**`)

    const api = viewerPage.request

    // Create lane — 403
    const createLaneResp = await api.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Viewer Lane' },
    })
    expect(createLaneResp.status(), 'create lane should be 403').toBe(403)

    // Rename lane — 403
    const renameLaneResp = await api.patch(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}`,
      { data: { name: 'Renamed' } },
    )
    expect(renameLaneResp.status(), 'rename lane should be 403').toBe(403)

    // Move lane — 403
    const moveLaneResp = await api.post(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/move`,
      { data: { targetPosition: 1, expectedVersion: lane.version } },
    )
    expect(moveLaneResp.status(), 'move lane should be 403').toBe(403)

    // Delete lane — 403
    const deleteLaneResp = await api.delete(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}`,
    )
    expect(deleteLaneResp.status(), 'delete lane should be 403').toBe(403)

    // Create card — 403
    const createCardResp = await api.post(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/cards`,
      { data: { title: 'Viewer Card' } },
    )
    expect(createCardResp.status(), 'create card should be 403').toBe(403)

    // Update card — 403
    const updateCardResp = await api.patch(
      `${API_BASE}/api/v1/boards/${board.id}/cards/${card.id}`,
      { data: { title: 'Updated' } },
    )
    expect(updateCardResp.status(), 'update card should be 403').toBe(403)

    // Move card — 403
    const moveCardResp = await api.post(
      `${API_BASE}/api/v1/boards/${board.id}/cards/${card.id}/move`,
      { data: { targetLaneId: lane.id, targetPosition: 1, expectedVersion: card.version } },
    )
    expect(moveCardResp.status(), 'move card should be 403').toBe(403)

    // Delete card — 403
    const deleteCardResp = await api.delete(
      `${API_BASE}/api/v1/boards/${board.id}/cards/${card.id}`,
    )
    expect(deleteCardResp.status(), 'delete card should be 403').toBe(403)

    await viewerCtx.close()
  })
})
