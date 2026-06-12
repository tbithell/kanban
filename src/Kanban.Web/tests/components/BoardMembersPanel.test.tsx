import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import type { UseMutationResult } from '@tanstack/react-query'
import BoardMembersPanel from '../../src/components/board/BoardMembersPanel'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'

type BoardRole = 'Owner' | 'Member' | 'Viewer'

interface BoardMember {
  userId: string
  displayName: string
  role: BoardRole
  joinedAt: string
}

vi.mock('../../src/hooks/useBoardMembers', () => ({ useBoardMembers: vi.fn() }))
vi.mock('../../src/hooks/useInviteBoardMember', () => ({ useInviteBoardMember: vi.fn() }))
vi.mock('../../src/hooks/useChangeMemberRole', () => ({ useChangeMemberRole: vi.fn() }))
vi.mock('../../src/hooks/useRemoveBoardMember', () => ({ useRemoveBoardMember: vi.fn() }))

import { useBoardMembers } from '../../src/hooks/useBoardMembers'
import { useInviteBoardMember } from '../../src/hooks/useInviteBoardMember'
import { useChangeMemberRole } from '../../src/hooks/useChangeMemberRole'
import { useRemoveBoardMember } from '../../src/hooks/useRemoveBoardMember'

const mockUseBoardMembers = vi.mocked(useBoardMembers)
const mockUseInviteBoardMember = vi.mocked(useInviteBoardMember)
const mockUseChangeMemberRole = vi.mocked(useChangeMemberRole)
const mockUseRemoveBoardMember = vi.mocked(useRemoveBoardMember)

const twoMembers: BoardMember[] = [
  {
    userId: 'user-owner',
    displayName: 'Alice Owner',
    role: 'Owner',
    joinedAt: '2024-01-01T00:00:00Z',
  },
  {
    userId: 'user-member',
    displayName: 'Bob Member',
    role: 'Member',
    joinedAt: '2024-01-02T00:00:00Z',
  },
]

const idleMutation = {
  mutate: vi.fn(),
  mutateAsync: vi.fn(),
  isPending: false,
  isError: false,
  isSuccess: false,
  isIdle: true,
  error: null,
  data: undefined,
  reset: vi.fn(),
  status: 'idle' as const,
  failureCount: 0,
  failureReason: null,
  variables: undefined,
  context: undefined,
  submittedAt: 0,
} satisfies UseMutationResult<void, Error, unknown>

function renderPanel(boardId = 'board-1', callerRole: BoardRole = 'Owner') {
  const { Wrapper } = createQueryClientWrapper()
  return render(<BoardMembersPanel boardId={boardId} callerRole={callerRole} />, {
    wrapper: Wrapper,
  })
}

describe('BoardMembersPanel', () => {
  beforeEach(() => {
    mockUseBoardMembers.mockReturnValue({
      data: twoMembers,
      isPending: false,
      isSuccess: true,
      isError: false,
      error: null,
    } as ReturnType<typeof useBoardMembers>)
    mockUseInviteBoardMember.mockReturnValue(
      idleMutation as ReturnType<typeof useInviteBoardMember>
    )
    mockUseChangeMemberRole.mockReturnValue(idleMutation as ReturnType<typeof useChangeMemberRole>)
    mockUseRemoveBoardMember.mockReturnValue(
      idleMutation as ReturnType<typeof useRemoveBoardMember>
    )
  })

  it('renders a labelled list of board members', () => {
    renderPanel()
    expect(screen.getByRole('list', { name: /board members/i })).toBeInTheDocument()
    expect(screen.getAllByRole('listitem')).toHaveLength(2)
  })

  it('shows each member display name', () => {
    renderPanel()
    expect(screen.getByText('Alice Owner')).toBeInTheDocument()
    expect(screen.getByText('Bob Member')).toBeInTheDocument()
  })

  it('shows an Invite button for Owner role', () => {
    renderPanel('board-1', 'Owner')
    expect(screen.getByRole('button', { name: /invite/i })).toBeInTheDocument()
  })

  it('does not show Invite button for Member role', () => {
    renderPanel('board-1', 'Member')
    expect(screen.queryByRole('button', { name: /invite/i })).not.toBeInTheDocument()
  })

  it('does not show Invite button for Viewer role', () => {
    renderPanel('board-1', 'Viewer')
    expect(screen.queryByRole('button', { name: /invite/i })).not.toBeInTheDocument()
  })

  it('shows Remove buttons for Owner caller', () => {
    renderPanel('board-1', 'Owner')
    const removeButtons = screen.getAllByRole('button', { name: /remove/i })
    expect(removeButtons.length).toBeGreaterThan(0)
  })

  it('does not show Remove buttons for Member caller', () => {
    renderPanel('board-1', 'Member')
    expect(screen.queryByRole('button', { name: /remove/i })).not.toBeInTheDocument()
  })

  it('shows role selector for Owner caller', () => {
    renderPanel('board-1', 'Owner')
    const selectors = screen.getAllByRole('combobox', { name: /role/i })
    expect(selectors.length).toBeGreaterThan(0)
  })

  it('shows loading spinner when data is pending', () => {
    mockUseBoardMembers.mockReturnValue({
      data: undefined,
      isPending: true,
      isSuccess: false,
      isError: false,
      error: null,
    } as ReturnType<typeof useBoardMembers>)
    renderPanel()
    expect(screen.getByRole('progressbar')).toBeInTheDocument()
  })

  it('hides Remove button for the only owner — last-owner guard in UI', () => {
    mockUseBoardMembers.mockReturnValue({
      data: [twoMembers[0]],
      isPending: false,
      isSuccess: true,
      isError: false,
      error: null,
    } as ReturnType<typeof useBoardMembers>)
    renderPanel('board-1', 'Owner')
    expect(screen.queryByRole('button', { name: /remove/i })).not.toBeInTheDocument()
  })
})
