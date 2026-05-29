import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import type { ReactNode } from 'react'
import AcceptInvitePage from '../../src/pages/AcceptInvitePage'
import * as useCurrentUserModule from '../../src/hooks/useCurrentUser'
import * as useAcceptInviteModule from '../../src/hooks/useAcceptInvite'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'

vi.mock('../../src/hooks/useCurrentUser')
vi.mock('../../src/hooks/useAcceptInvite')

const mockUseCurrentUser = vi.mocked(useCurrentUserModule.useCurrentUser)
const mockUseAcceptInvite = vi.mocked(useAcceptInviteModule.useAcceptInvite)

const idleMutation = {
  mutate: vi.fn(),
  data: undefined,
  error: null,
  isPending: false,
  isIdle: true,
  isSuccess: false,
  isError: false,
  variables: undefined,
  context: undefined,
  failureCount: 0,
  failureReason: null,
  isPaused: false,
  status: 'idle' as const,
  submittedAt: 0,
  mutateAsync: vi.fn(),
  reset: vi.fn(),
}

const authenticatedUser = {
  user: undefined,
  isLoading: false,
  isUnauthenticated: false,
  isNotRegistered: true,
  isError: true,
}

function renderPage(token = 'testtoken123') {
  const { Wrapper } = createQueryClientWrapper()

  function RouterWrapper({ children }: { children: ReactNode }) {
    return (
      <Wrapper>
        <MemoryRouter initialEntries={[`/accept/${token}`]}>{children}</MemoryRouter>
      </Wrapper>
    )
  }

  return render(
    <Routes>
      <Route path="/accept/:token" element={<AcceptInvitePage />} />
      <Route path="/" element={<div>Home Page</div>} />
    </Routes>,
    { wrapper: RouterWrapper }
  )
}

beforeEach(() => {
  mockUseCurrentUser.mockReturnValue({
    user: undefined,
    isLoading: false,
    isUnauthenticated: false,
    isNotRegistered: true,
    isError: true,
  })
  mockUseAcceptInvite.mockReturnValue({ ...idleMutation, mutate: vi.fn() })
})

describe('AcceptInvitePage', () => {
  it('unauthenticated state renders Accept & Sign in with Google button', () => {
    mockUseCurrentUser.mockReturnValue({
      user: undefined,
      isLoading: false,
      isUnauthenticated: true,
      isNotRegistered: false,
      isError: true,
    })

    renderPage()

    expect(screen.getByRole('link', { name: /accept.*sign in.*google/i })).toBeInTheDocument()
  })

  it('authenticated state auto-calls accept mutation with the token', async () => {
    const mockMutate = vi.fn()
    mockUseAcceptInvite.mockReturnValue({ ...idleMutation, mutate: mockMutate })
    mockUseCurrentUser.mockReturnValue(authenticatedUser)

    renderPage('myspecialtoken')

    await waitFor(() => {
      expect(mockMutate).toHaveBeenCalledWith('myspecialtoken')
    })
  })

  it('on successful acceptance navigates to home', async () => {
    mockUseCurrentUser.mockReturnValue(authenticatedUser)
    mockUseAcceptInvite.mockReturnValue({
      ...idleMutation,
      isSuccess: true,
      isIdle: false,
      status: 'success' as const,
      data: {
        id: 'user-1',
        email: 'invitee@example.com',
        displayName: 'Invitee',
        systemRole: 'standard',
        registeredAt: new Date().toISOString(),
        lastSignInAt: null,
      },
    })

    renderPage()

    await waitFor(() => {
      expect(screen.getByText('Home Page')).toBeInTheDocument()
    })
  })

  it('410 error renders invitation no longer valid message', () => {
    mockUseCurrentUser.mockReturnValue(authenticatedUser)
    mockUseAcceptInvite.mockReturnValue({
      ...idleMutation,
      isError: true,
      isIdle: false,
      status: 'error' as const,
      error: { status: 410, code: 'invite.invalid', title: 'This invitation is no longer valid.' },
    })

    renderPage()

    expect(screen.getByText(/no longer valid/i)).toBeInTheDocument()
  })

  it('422 error renders issued to a different email message', () => {
    mockUseCurrentUser.mockReturnValue(authenticatedUser)
    mockUseAcceptInvite.mockReturnValue({
      ...idleMutation,
      isError: true,
      isIdle: false,
      status: 'error' as const,
      error: {
        status: 422,
        code: 'invite.email_mismatch',
        title: 'This invitation was issued to a different email address.',
      },
    })

    renderPage()

    expect(screen.getByText(/different email/i)).toBeInTheDocument()
  })
})
