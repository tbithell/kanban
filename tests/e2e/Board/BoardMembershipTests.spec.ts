import { test, expect } from '@playwright/test'
import { ADMIN_AUTH } from '../auth.helpers'

const API_BASE = process.env.PLAYWRIGHT_API_BASE ?? 'http://localhost:5077'
const WEB_BASE = process.env.PLAYWRIGHT_WEB_BASE ?? 'http://localhost:5173'

test.describe('US4 — Board owner invites and manages board members', () => {
  test.use({ storageState: ADMIN_AUTH })

  test('scenario 1 — board owner sees Members button on the board page', async ({
    page,
    request,
  }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Members Button Board ${Date.now()}` },
    })
    expect(boardResp.ok(), `Board creation failed: ${boardResp.status()}`).toBeTruthy()
    const board = await boardResp.json()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByRole('heading', { name: board.name })).toBeVisible({ timeout: 10_000 })

    await expect(page.getByRole('button', { name: /members/i })).toBeVisible()
  })

  test('scenario 2 — owner opens members panel and sees the member list', async ({
    page,
    request,
  }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Members Panel Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByRole('heading', { name: board.name })).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /members/i }).click()

    await expect(page.getByRole('list', { name: /board members/i })).toBeVisible({ timeout: 5_000 })
    await expect(page.getByRole('listitem').first()).toBeVisible()
  })

  test('scenario 3 — owner invites a new member by email', async ({ page, request }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Invite New Member Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()
    const inviteeEmail = `e2e-invitee-${Date.now()}@example.com`

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByRole('heading', { name: board.name })).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /members/i }).click()
    await expect(page.getByRole('list', { name: /board members/i })).toBeVisible({ timeout: 5_000 })

    await page.getByRole('button', { name: /invite/i }).click()
    await page.getByRole('textbox', { name: /email/i }).fill(inviteeEmail)
    await page.getByRole('button', { name: /send invite/i }).click()

    await expect(page.getByRole('alert')).toContainText(/invite sent|success/i, { timeout: 5_000 })
  })

  test('scenario 4 — owner changes a member role', async ({ page, request, browser }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Change Role Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()
    const memberEmail = `e2e-change-role-${Date.now()}@example.com`

    const seedResp = await request.post(`${API_BASE}/api/v1/dev/seed-board-member`, {
      data: { email: memberEmail, boardId: board.id, role: 'Member' },
    })
    expect(seedResp.ok(), `Seed member failed: ${seedResp.status()}`).toBeTruthy()
    const seededMember = await seedResp.json()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByRole('heading', { name: board.name })).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /members/i }).click()
    await expect(page.getByRole('list', { name: /board members/i })).toBeVisible({ timeout: 5_000 })

    const memberRow = page.getByRole('listitem').filter({ hasText: memberEmail })
    const roleChangeResp = page.waitForResponse(
      r => r.url().includes(`/boards/${board.id}/members`) && r.status() === 200,
    )
    await memberRow.getByRole('combobox', { name: /role/i }).selectOption('Viewer')
    await roleChangeResp

    await expect(memberRow).toContainText(/viewer/i, { timeout: 5_000 })

    // Verify via API
    const membersResp = await request.get(`${API_BASE}/api/v1/boards/${board.id}/members`)
    const members = await membersResp.json()
    const updated = members.find((m: { userId: string }) => m.userId === seededMember.userId)
    expect(updated?.role).toBe('Viewer')
  })

  test('scenario 5 — owner removes a member', async ({ page, request }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Remove Member Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()
    const memberEmail = `e2e-remove-${Date.now()}@example.com`

    const seedResp = await request.post(`${API_BASE}/api/v1/dev/seed-board-member`, {
      data: { email: memberEmail, boardId: board.id, role: 'Member' },
    })
    expect(seedResp.ok(), `Seed member failed: ${seedResp.status()}`).toBeTruthy()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByRole('heading', { name: board.name })).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /members/i }).click()
    await expect(page.getByRole('list', { name: /board members/i })).toBeVisible({ timeout: 5_000 })

    const memberRow = page.getByRole('listitem').filter({ hasText: memberEmail })
    await memberRow.getByRole('button', { name: /remove/i }).click()

    await expect(memberRow).not.toBeVisible({ timeout: 5_000 })
  })

  test('scenario 6 — member-role user does not see management controls', async ({
    page,
    request,
    browser,
  }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Member View Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()
    const memberEmail = `e2e-member-view-${Date.now()}@example.com`

    const seedResp = await request.post(`${API_BASE}/api/v1/dev/seed-board-member`, {
      data: { email: memberEmail, boardId: board.id, role: 'Member' },
    })
    expect(seedResp.ok(), `Seed member failed: ${seedResp.status()}`).toBeTruthy()

    const memberCtx = await browser.newContext()
    const memberPage = await memberCtx.newPage()
    await memberPage.goto(
      `${API_BASE}/api/v1/dev/authenticate?email=${encodeURIComponent(memberEmail)}&displayName=Member+User`,
    )
    await memberPage.waitForURL(`${WEB_BASE}/**`)
    await memberPage.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(memberPage.getByRole('heading', { name: board.name })).toBeVisible({
      timeout: 10_000,
    })

    // Members button should not be visible for non-owner
    await expect(memberPage.getByRole('button', { name: /members/i })).not.toBeVisible()

    await memberCtx.close()
  })

  test('scenario 7 — removing the last owner is rejected', async ({ page, request }) => {
    const boardResp = await request.post(`${API_BASE}/api/v1/boards`, {
      data: { name: `Last Owner Board ${Date.now()}` },
    })
    expect(boardResp.ok()).toBeTruthy()
    const board = await boardResp.json()

    await page.goto(`${WEB_BASE}/boards/${board.id}`)
    await expect(page.getByRole('heading', { name: board.name })).toBeVisible({ timeout: 10_000 })

    await page.getByRole('button', { name: /members/i }).click()
    await expect(page.getByRole('list', { name: /board members/i })).toBeVisible({ timeout: 5_000 })

    // The owner row should not have a Remove button (guarded in UI when only 1 owner)
    // OR it shows an error if attempted via API
    const ownerRows = page.getByRole('listitem').filter({ hasText: /owner/i })
    await expect(ownerRows.first()).toBeVisible()
    await expect(ownerRows.first().getByRole('button', { name: /remove/i })).not.toBeVisible()
  })
})
