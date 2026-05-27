import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import App from '../../src/App'
import { useCurrentUser } from '../../src/hooks/useCurrentUser'

vi.mock('../../src/hooks/useCurrentUser', () => ({
  useCurrentUser: vi.fn(),
}))

const mockUseCurrentUser = vi.mocked(useCurrentUser)

const unauthenticatedState = {
  user: undefined,
  isLoading: false,
  isUnauthenticated: true,
  isNotRegistered: false,
  isError: false,
}

const notRegisteredState = {
  user: undefined,
  isLoading: false,
  isUnauthenticated: false,
  isNotRegistered: true,
  isError: false,
}

const authenticatedState = {
  user: {
    id: '00000000-0000-0000-0000-000000000001',
    email: 'user@example.com',
    displayName: 'Test User',
    systemRole: 'standard' as const,
    registeredAt: '2024-01-01T00:00:00Z',
    lastSignInAt: null,
  },
  isLoading: false,
  isUnauthenticated: false,
  isNotRegistered: false,
  isError: false,
}

describe('App routing', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('redirects unauthenticated users visiting / to /signin', () => {
    mockUseCurrentUser.mockReturnValue(unauthenticatedState)

    render(
      <MemoryRouter initialEntries={['/']}>
        <App />
      </MemoryRouter>
    )

    expect(screen.getByRole('heading', { name: /sign in to kanban/i })).toBeInTheDocument()
  })

  it('redirects not-registered users visiting / to /not-registered', () => {
    mockUseCurrentUser.mockReturnValue(notRegisteredState)

    render(
      <MemoryRouter initialEntries={['/']}>
        <App />
      </MemoryRouter>
    )

    expect(
      screen.getByRole('heading', { name: /not registered|access denied/i })
    ).toBeInTheDocument()
  })

  it('renders the landing page for authenticated users at /', () => {
    mockUseCurrentUser.mockReturnValue(authenticatedState)

    render(
      <MemoryRouter initialEntries={['/']}>
        <App />
      </MemoryRouter>
    )

    expect(screen.queryByRole('heading', { name: /sign in to kanban/i })).not.toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: /not registered|access denied/i })
    ).not.toBeInTheDocument()
  })

  it('renders SignInPage at /signin regardless of auth state', () => {
    mockUseCurrentUser.mockReturnValue(unauthenticatedState)

    render(
      <MemoryRouter initialEntries={['/signin']}>
        <App />
      </MemoryRouter>
    )

    expect(screen.getByRole('heading', { name: /sign in to kanban/i })).toBeInTheDocument()
  })

  it('renders NotRegisteredPage at /not-registered', () => {
    mockUseCurrentUser.mockReturnValue(notRegisteredState)

    render(
      <MemoryRouter initialEntries={['/not-registered']}>
        <App />
      </MemoryRouter>
    )

    expect(
      screen.getByRole('heading', { name: /not registered|access denied/i })
    ).toBeInTheDocument()
  })
})
