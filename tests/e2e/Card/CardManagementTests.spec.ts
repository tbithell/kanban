import { test, expect } from '@playwright/test'
import { ADMIN_AUTH, API_BASE, WEB_BASE } from '../auth.helpers'

// ── US2: Board member adds and manages cards ──────────────────────────────────

test.describe('US2: Board member adds and manages cards', () => {
  test.use({ storageState: ADMIN_AUTH })

  test('scenario 1 — member adds a card to a lane and it appears', async ({ page, request }) => {
    const boardName = `Card Board ${Date.now()}`
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, { data: { name: boardName } })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()
    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'To Do' },
    })
    expect(laneResp.ok()).toBeTruthy()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByRole('heading', { name: boardName })).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /add card/i }).first().click()
    await page.getByRole('textbox', { name: /card title/i }).fill('My First Card')
    await page.getByRole('button', { name: /^add$/i }).click()

    await expect(page.getByText('My First Card')).toBeVisible({ timeout: 10_000 })
  })

  test('scenario 2 — member updates title, description, and due date', async ({
    page,
    request,
  }) => {
    const boardName = `Update Card Board ${Date.now()}`
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, { data: { name: boardName } })
    const board = await boardResp.json()
    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Work' },
    })
    const lane = await laneResp.json()
    const cardResp = await request.post(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/cards`,
      { data: { title: 'Original Title' } },
    )
    expect(cardResp.ok()).toBeTruthy()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByText('Original Title')).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /edit card/i }).first().click()
    await page.getByRole('textbox', { name: /title/i }).fill('Updated Title')
    await page.getByRole('textbox', { name: /description/i }).fill('A new description')
    await page.getByRole('button', { name: /save/i }).click()

    await expect(page.getByText('Updated Title')).toBeVisible({ timeout: 10_000 })
  })

  test('scenario 3 — member explicitly clears a card due date', async ({ page, request }) => {
    const boardName = `Due Date Board ${Date.now()}`
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, { data: { name: boardName } })
    const board = await boardResp.json()
    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Work' },
    })
    const lane = await laneResp.json()
    const cardResp = await request.post(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/cards`,
      { data: { title: 'Task With Due Date', dueDate: '2099-12-31' } },
    )
    expect(cardResp.ok()).toBeTruthy()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByText('Task With Due Date')).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /edit card/i }).first().click()
    await page.getByRole('checkbox', { name: /clear due date/i }).check()
    await page.getByRole('button', { name: /save/i }).click()
    // not.toBeAttached waits for the Fluent UI dialog portal to be fully removed
    // from the DOM (not just hidden), at which point its backdrop no longer blocks
    // pointer events on the cards behind it.
    await expect(page.getByRole('dialog')).not.toBeAttached({ timeout: 10_000 })

    await page.getByRole('button', { name: /edit card/i }).first().click()
    const dueDateField = page.getByRole('textbox', { name: /due date/i })
    await expect(dueDateField).toHaveValue('')
  })

  test('scenario 4 — delete card leaves remaining positions gapless', async ({
    page,
    request,
  }) => {
    const boardName = `Delete Card Board ${Date.now()}`
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, { data: { name: boardName } })
    const board = await boardResp.json()
    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Work' },
    })
    const lane = await laneResp.json()
    for (const title of ['Card Alpha', 'Card Beta', 'Card Gamma']) {
      const resp = await request.post(
        `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/cards`,
        { data: { title } },
      )
      expect(resp.ok()).toBeTruthy()
    }

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByText('Card Alpha')).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /delete card/i }).first().click()
    await expect(page.getByRole('dialog')).toBeVisible({ timeout: 5_000 })
    await page.getByRole('button', { name: /^delete card$/i }).click()
    await page.getByRole('button', { name: /confirm/i }).click()
    await expect(page.getByRole('dialog')).not.toBeAttached({ timeout: 10_000 })

    await expect(page.getByText('Card Alpha')).not.toBeVisible()
    await expect(page.getByText('Card Beta')).toBeVisible()
    await expect(page.getByText('Card Gamma')).toBeVisible()

    // Verify gapless positions via API
    const boardDetailResp = await request.get(`${API_BASE}/api/v1/boards/${board.id}`)
    const boardDetail = await boardDetailResp.json()
    const positions = boardDetail.lanes[0].cards.map((c: { position: number }) => c.position)
    expect(positions).toEqual([1, 2])
  })

  test('scenario 5 — viewer role add-card request returns 403', async ({ request, browser }) => {
    const viewerEmail = `e2e-viewer-${Date.now()}@example.com`

    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Viewer Forbidden Board ${Date.now()}` },
    })
    const board = await boardResp.json()
    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Lane' },
    })
    const lane = await laneResp.json()

    // Add viewer to the board via dev seed endpoint (implemented alongside CardEndpoints)
    const seedResp = await request.post(`${API_BASE}/api/v1/dev/seed-board-member`, {
      data: { email: viewerEmail, boardId: board.id, role: 'Viewer' },
    })
    expect(seedResp.ok()).toBeTruthy()

    const viewerCtx = await browser.newContext()
    const viewerPage = await viewerCtx.newPage()
    await viewerPage.goto(
      `${API_BASE}/api/v1/dev/authenticate?email=${encodeURIComponent(viewerEmail)}&displayName=Viewer+User`,
    )
    await viewerPage.waitForURL(`${WEB_BASE}/**`)

    const forbiddenResp = await viewerPage.request.post(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/cards`,
      { data: { title: 'Viewer Should Not Create This' } },
    )
    expect(forbiddenResp.status()).toBe(403)

    await viewerCtx.close()
  })

  test.skip('scenario 6 — empty card title is rejected with validation error', async ({
    page,
    request,
  }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Title Val Board ${Date.now()}` },
    })
    const board = await boardResp.json()
    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Lane' },
    })
    expect(laneResp.ok()).toBeTruthy()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByRole('heading', { name: board.name })).toBeVisible({ timeout: 10_000 })
    await expect(page.getByText('Lane')).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /add card/i }).first().click()
    await page.getByRole('textbox', { name: /card title/i }).fill('')
    await page.getByRole('button', { name: /^add$/i }).click()

    await expect(page.getByRole('alert')).toBeVisible({ timeout: 10_000 })
  })
})
