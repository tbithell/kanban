import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import App from '../../src/App'
import { useCurrentUser } from '../../src/hooks/useCurrentUser'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'

vi.mock('../../src/hooks/useCurrentUser', () => ({
  useCurrentUser: vi.fn(),
}))
vi.mock('../../src/hooks/useBoards', () => ({
  useBoards: vi.fn(() => ({
    data: [],
    isPending: false,
    isSuccess: true,
    isError: false,
    error: null,
  })),
}))
vi.mock('../../src/hooks/useDeleteBoard', () => ({
  useDeleteBoard: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
}))
vi.mock('../../src/hooks/useCreateBoard', () => ({
  useCreateBoard: vi.fn(() => ({ mutate: vi.fn(), isPending: false })),
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

  function renderApp(initialPath: string) {
    const { Wrapper } = createQueryClientWrapper()
    return render(
      <Wrapper>
        <MemoryRouter initialEntries={[initialPath]}>
          <App />
        </MemoryRouter>
      </Wrapper>
    )
  }

  it('redirects unauthenticated users visiting / to /signin', () => {
    mockUseCurrentUser.mockReturnValue(unauthenticatedState)
    renderApp('/')
    expect(screen.getByRole('heading', { name: /sign in to kanban/i })).toBeInTheDocument()
  })

  it('redirects not-registered users visiting / to /not-registered', () => {
    mockUseCurrentUser.mockReturnValue(notRegisteredState)
    renderApp('/')
    expect(
      screen.getByRole('heading', { name: /not registered|access denied/i })
    ).toBeInTheDocument()
  })

  it('renders the landing page for authenticated users at /', () => {
    mockUseCurrentUser.mockReturnValue(authenticatedState)
    renderApp('/')
    expect(screen.queryByRole('heading', { name: /sign in to kanban/i })).not.toBeInTheDocument()
    expect(
      screen.queryByRole('heading', { name: /not registered|access denied/i })
    ).not.toBeInTheDocument()
  })

  it('renders SignInPage at /signin regardless of auth state', () => {
    mockUseCurrentUser.mockReturnValue(unauthenticatedState)
    renderApp('/signin')
    expect(screen.getByRole('heading', { name: /sign in to kanban/i })).toBeInTheDocument()
  })

  it('renders NotRegisteredPage at /not-registered', () => {
    mockUseCurrentUser.mockReturnValue(notRegisteredState)
    renderApp('/not-registered')
    expect(
      screen.getByRole('heading', { name: /not registered|access denied/i })
    ).toBeInTheDocument()
  })
})
