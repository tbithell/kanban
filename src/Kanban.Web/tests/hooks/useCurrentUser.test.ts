import { renderHook, waitFor } from '@testing-library/react'
import { describe, it, expect, beforeAll, afterEach, afterAll } from 'vitest'
import { http, HttpResponse } from 'msw'
import { setupServer } from 'msw/node'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'
import { useCurrentUser } from '../../src/hooks/useCurrentUser'

const server = setupServer()

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }))
afterEach(() => server.resetHandlers())
afterAll(() => server.close())

const mockUser = {
  id: '00000000-0000-0000-0000-000000000001',
  email: 'admin@example.com',
  displayName: 'Test Admin',
  systemRole: 'admin',
  registeredAt: '2024-01-01T00:00:00Z',
  lastSignInAt: '2024-01-02T00:00:00Z',
}

describe('useCurrentUser', () => {
  it('returns user data when the API responds with 200', async () => {
    server.use(http.get('/api/v1/auth/me', () => HttpResponse.json(mockUser)))

    const { Wrapper } = createQueryClientWrapper()
    const { result } = renderHook(() => useCurrentUser(), { wrapper: Wrapper })

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.user).toEqual(mockUser)
    expect(result.current.isUnauthenticated).toBe(false)
    expect(result.current.isNotRegistered).toBe(false)
  })

  it('sets isUnauthenticated when the API responds with 401', async () => {
    server.use(http.get('/api/v1/auth/me', () => new HttpResponse(null, { status: 401 })))

    const { Wrapper } = createQueryClientWrapper()
    const { result } = renderHook(() => useCurrentUser(), { wrapper: Wrapper })

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.user).toBeUndefined()
    expect(result.current.isUnauthenticated).toBe(true)
    expect(result.current.isNotRegistered).toBe(false)
  })

  it('sets isNotRegistered when the API responds with 403', async () => {
    server.use(http.get('/api/v1/auth/me', () => new HttpResponse(null, { status: 403 })))

    const { Wrapper } = createQueryClientWrapper()
    const { result } = renderHook(() => useCurrentUser(), { wrapper: Wrapper })

    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(result.current.user).toBeUndefined()
    expect(result.current.isNotRegistered).toBe(true)
    expect(result.current.isUnauthenticated).toBe(false)
  })
})
