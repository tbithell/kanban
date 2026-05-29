import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import InviteUserDialog from '../../src/components/admin/InviteUserDialog'
import { useIssueInvite } from '../../src/hooks/useIssueInvite'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'

vi.mock('../../src/hooks/useIssueInvite', () => ({
  useIssueInvite: vi.fn(),
}))

const mockUseIssueInvite = vi.mocked(useIssueInvite)

function renderDialog(open = true) {
  const { Wrapper } = createQueryClientWrapper()
  return render(<InviteUserDialog open={open} onDismiss={vi.fn()} />, { wrapper: Wrapper })
}

beforeEach(() => {
  mockUseIssueInvite.mockReturnValue({
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
    status: 'idle',
    submittedAt: 0,
    mutateAsync: vi.fn(),
    reset: vi.fn(),
  })
})

describe('InviteUserDialog', () => {
  it('renders email field with accessible label', () => {
    renderDialog()
    expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
  })

  it('submit button is disabled when email field is empty', () => {
    renderDialog()
    const emailInput = screen.getByLabelText(/email address/i)
    expect(emailInput).toHaveValue('')
    const submitBtn = screen.getByRole('button', { name: /send invitation/i })
    expect(submitBtn).toBeDisabled()
  })

  it('successful mutation renders copyable redemption link', async () => {
    mockUseIssueInvite.mockReturnValue({
      mutate: vi.fn(),
      data: {
        token: 'abc123token',
        redemptionLink: 'http://localhost:5173/accept/abc123token',
        expiresAt: new Date(Date.now() + 7 * 86400000).toISOString(),
      },
      error: null,
      isPending: false,
      isIdle: false,
      isSuccess: true,
      isError: false,
      variables: 'newinvitee@example.com',
      context: undefined,
      failureCount: 0,
      failureReason: null,
      isPaused: false,
      status: 'success',
      submittedAt: 0,
      mutateAsync: vi.fn(),
      reset: vi.fn(),
    })

    renderDialog()

    expect(screen.getByRole('textbox', { name: /redemption link/i })).toHaveValue(
      'http://localhost:5173/accept/abc123token'
    )
  })

  it('409 conflict error renders "already registered" inline message', async () => {
    mockUseIssueInvite.mockReturnValue({
      mutate: vi.fn(),
      data: undefined,
      error: {
        status: 409,
        code: 'invite.already_registered',
        title: 'This email address is already registered.',
      },
      isPending: false,
      isIdle: false,
      isSuccess: false,
      isError: true,
      variables: 'existing@example.com',
      context: undefined,
      failureCount: 1,
      failureReason: null,
      isPaused: false,
      status: 'error',
      submittedAt: 0,
      mutateAsync: vi.fn(),
      reset: vi.fn(),
    })

    renderDialog()

    expect(screen.getByText(/already registered/i)).toBeInTheDocument()
  })

  it('422 validation error renders email validation message', async () => {
    mockUseIssueInvite.mockReturnValue({
      mutate: vi.fn(),
      data: undefined,
      error: { status: 422, code: 'validation.failed', title: 'Validation failed.' },
      isPending: false,
      isIdle: false,
      isSuccess: false,
      isError: true,
      variables: 'not-an-email',
      context: undefined,
      failureCount: 1,
      failureReason: null,
      isPaused: false,
      status: 'error',
      submittedAt: 0,
      mutateAsync: vi.fn(),
      reset: vi.fn(),
    })

    renderDialog()

    expect(screen.getByText(/valid email/i)).toBeInTheDocument()
  })
})
