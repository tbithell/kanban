import { test, expect } from '@playwright/test'
import { ADMIN_AUTH, API_BASE, WEB_BASE } from '../auth.helpers'

// ── US3: Member reorders cards and lanes via drag-and-drop ────────────────────

test.describe('US3: Member reorders cards and lanes via drag-and-drop', () => {
  test.use({ storageState: ADMIN_AUTH })

  test('scenario 1 — card same-lane reorder persists after reload', async ({ page, request }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `DnD Same-Lane Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()
    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'To Do' },
    })
    const lane = await laneResp.json()
    for (const title of ['Alpha', 'Beta', 'Gamma']) {
      const resp = await request.post(
        `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/cards`,
        { data: { title } },
      )
      expect(resp.ok()).toBeTruthy()
    }

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByText('Alpha')).toBeVisible({ timeout: 10_000 })

    // Drag Alpha card to the bottom (position 3)
    const alphaCard = page.getByText('Alpha').first()
    const gammaCard = page.getByText('Gamma').first()
    await alphaCard.dragTo(gammaCard)

    // Confirm reorder is visible immediately
    await expect(page.getByText('Beta')).toBeVisible({ timeout: 5_000 })

    // Reload and confirm persisted
    await page.reload()
    await expect(page.getByText('Alpha')).toBeVisible({ timeout: 10_000 })
    const boardDetailResp = await request.get(`${API_BASE}/api/v1/boards/${board.id}`)
    const boardDetail = await boardDetailResp.json()
    const cardTitles = boardDetail.lanes[0].cards.map((c: { title: string }) => c.title)
    // Alpha should no longer be at position 1
    expect(cardTitles[0]).not.toBe('Alpha')
  })

  test('scenario 2 — card cross-lane move persists after reload', async ({ page, request }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `DnD Cross-Lane Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()
    const lane1Resp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'To Do' },
    })
    const lane1 = await lane1Resp.json()
    const lane2Resp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Done' },
    })
    const lane2 = await lane2Resp.json()
    const cardResp = await request.post(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane1.id}/cards`,
      { data: { title: 'Cross Card' } },
    )
    expect(cardResp.ok()).toBeTruthy()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByText('Cross Card')).toBeVisible({ timeout: 10_000 })

    const card = page.getByText('Cross Card').first()
    const doneLaneHeading = page.getByRole('heading', { name: /Done/i })
    await card.dragTo(doneLaneHeading)

    // Wait for optimistic update or server response
    await page.waitForTimeout(500)

    // Reload and confirm the card is now in Done lane
    await page.reload()
    await expect(page.getByText('Cross Card')).toBeVisible({ timeout: 10_000 })
    const boardDetailResp = await request.get(`${API_BASE}/api/v1/boards/${board.id}`)
    const boardDetail = await boardDetailResp.json()
    const doneLane = boardDetail.lanes.find((l: { id: string }) => l.id === lane2.id)
    expect(doneLane?.cards.map((c: { title: string }) => c.title)).toContain('Cross Card')
  })

  test('scenario 3 — lane reorder persists after reload', async ({ page, request }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `DnD Lane Reorder Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()
    for (const name of ['First', 'Second']) {
      await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, { data: { name } })
    }

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByRole('heading', { name: /First/i })).toBeVisible({ timeout: 10_000 })

    const firstHeading = page.getByRole('heading', { name: /First/i })
    const secondHeading = page.getByRole('heading', { name: /Second/i })
    await firstHeading.dragTo(secondHeading)

    await page.waitForTimeout(500)

    // Reload and confirm order changed
    await page.reload()
    await expect(page.getByRole('heading', { name: /First/i })).toBeVisible({ timeout: 10_000 })
    const boardDetailResp = await request.get(`${API_BASE}/api/v1/boards/${board.id}`)
    const boardDetail = await boardDetailResp.json()
    const laneNames = boardDetail.lanes.map((l: { name: string }) => l.name)
    expect(laneNames).toHaveLength(2)
    // Positions should have changed
    expect(laneNames[0]).toBe('Second')
  })

  test('scenario 4 — keyboard drag with Space/arrow/Space moves a card', async ({
    page,
    request,
  }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `DnD Keyboard Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()
    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Keyboard Lane' },
    })
    const lane = await laneResp.json()
    for (const title of ['Card One', 'Card Two']) {
      await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/cards`, {
        data: { title },
      })
    }

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByText('Card One')).toBeVisible({ timeout: 10_000 })

    // Focus first card's drag handle and use keyboard
    const firstCard = page.getByText('Card One').first()
    await firstCard.focus()
    // Space to pick up
    await page.keyboard.press('Space')
    // Arrow down to move to position 2
    await page.keyboard.press('ArrowDown')
    // Space to drop
    await page.keyboard.press('Space')

    // Confirm card moved (Card Two should now be first)
    await page.waitForTimeout(500)
    const boardDetailResp = await request.get(`${API_BASE}/api/v1/boards/${board.id}`)
    const boardDetail = await boardDetailResp.json()
    const cardTitles = boardDetail.lanes[0].cards.map((c: { title: string }) => c.title)
    // Positions should have swapped
    expect(cardTitles[0]).toBe('Card Two')
  })

  test('scenario 5 — viewer role cannot initiate a drag', async ({ request, browser }) => {
    const viewerEmail = `e2e-dnd-viewer-${Date.now()}@example.com`

    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Viewer DnD Board ${Date.now()}` },
    })
    const board = await boardResp.json()
    const laneResp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Lane' },
    })
    const lane = await laneResp.json()
    await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes/${lane.id}/cards`, {
      data: { title: 'Viewer Card' },
    })

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
    await viewerPage.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(viewerPage.getByText('Viewer Card')).toBeVisible({ timeout: 10_000 })

    // Viewer: no drag handle attribute should be present on cards
    const dragHandles = await viewerPage.$$('[data-drag-handle]')
    expect(dragHandles).toHaveLength(0)

    await viewerCtx.close()
  })

  test('scenario 6 — concurrent moves for same card result in one 409', async ({ request }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `DnD Concurrent Board ${Date.now()}` },
    })
    const board = await boardResp.json()
    const lane1Resp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Lane A' },
    })
    const lane1 = await lane1Resp.json()
    const lane2Resp = await request.post(`${API_BASE}/api/v1/boards/${board.id}/lanes`, {
      data: { name: 'Lane B' },
    })
    const lane2 = await lane2Resp.json()
    const cardResp = await request.post(
      `${API_BASE}/api/v1/boards/${board.id}/lanes/${lane1.id}/cards`,
      { data: { title: 'Contended Card' } },
    )
    const card = await cardResp.json()

    // Two simultaneous moves for the same card — both use version=1
    const [r1, r2] = await Promise.all([
      request.post(`${API_BASE}/api/v1/boards/${board.id}/cards/${card.id}/move`, {
        data: { targetLaneId: lane2.id, targetPosition: 1, expectedVersion: 1 },
      }),
      request.post(`${API_BASE}/api/v1/boards/${board.id}/cards/${card.id}/move`, {
        data: { targetLaneId: lane2.id, targetPosition: 1, expectedVersion: 1 },
      }),
    ])

    const statuses = [r1.status(), r2.status()].sort()
    // Exactly one winner (200) and one loser (409)
    expect(statuses).toEqual([200, 409])
  })
})
