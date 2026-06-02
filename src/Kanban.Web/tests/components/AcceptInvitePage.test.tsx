import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import type { ReactNode } from 'react'
import type { UseMutationResult } from '@tanstack/react-query'
import AcceptInvitePage from '../../src/pages/AcceptInvitePage'
import * as useCurrentUserModule from '../../src/hooks/useCurrentUser'
import * as useAcceptInviteModule from '../../src/hooks/useAcceptInvite'
import type { AcceptInviteResponse, AcceptInviteError } from '../../src/hooks/useAcceptInvite'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'

vi.mock('../../src/hooks/useCurrentUser')
vi.mock('../../src/hooks/useAcceptInvite')

const mockUseCurrentUser = vi.mocked(useCurrentUserModule.useCurrentUser)
const mockUseAcceptInvite = vi.mocked(useAcceptInviteModule.useAcceptInvite)

// satisfies catches API surface changes at compile time — a renamed field or changed
// argument type breaks this stub, not silently at runtime.
const idleMutation = {
  mutate: vi.fn(),
  mutateAsync: vi.fn().mockResolvedValue(undefined),
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
  reset: vi.fn(),
} satisfies UseMutationResult<AcceptInviteResponse, AcceptInviteError, string>

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
  mockUseAcceptInvite.mockReturnValue({ ...idleMutation })
})

describe('AcceptInvitePage', () => {
  it('renders a level-1 heading — WCAG AA gate', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument()
  })

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
    const mockMutateAsync = vi.fn().mockResolvedValue(undefined)
    mockUseAcceptInvite.mockReturnValue({ ...idleMutation, mutateAsync: mockMutateAsync })
    mockUseCurrentUser.mockReturnValue(authenticatedUser)

    renderPage('myspecialtoken')

    await waitFor(() => {
      expect(mockMutateAsync).toHaveBeenCalledWith('myspecialtoken')
    })
  })

  it('shows no alert in idle state', () => {
    mockUseCurrentUser.mockReturnValue(authenticatedUser)
    renderPage()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it('410 error renders invitation no longer valid message with alert role', () => {
    mockUseCurrentUser.mockReturnValue(authenticatedUser)
    mockUseAcceptInvite.mockReturnValue({
      ...idleMutation,
      isError: true,
      isIdle: false,
      status: 'error' as const,
      error: { status: 410, code: 'invite.invalid', title: 'This invitation is no longer valid.' },
    })

    renderPage()

    expect(screen.getByRole('alert')).toHaveTextContent(/no longer valid/i)
  })

  it('422 error renders issued to different email message with alert role', () => {
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

    expect(screen.getByRole('alert')).toHaveTextContent(/different email/i)
  })
})
