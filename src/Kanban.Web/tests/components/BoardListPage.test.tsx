import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import type { ReactNode } from 'react'
import type { UseMutationResult } from '@tanstack/react-query'
import BoardListPage from '../../src/pages/BoardListPage'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'

type BoardRole = 'Owner' | 'Member' | 'Viewer'

interface BoardSummary {
  id: string
  name: string
  laneCount: number
  cardCount: number
  callerRole: BoardRole
}

interface ApiError {
  status: number
  code?: string
  title?: string
}

vi.mock('../../src/hooks/useBoards', () => ({ useBoards: vi.fn() }))
vi.mock('../../src/hooks/useCreateBoard', () => ({ useCreateBoard: vi.fn() }))
vi.mock('../../src/hooks/useDeleteBoard', () => ({ useDeleteBoard: vi.fn() }))
vi.mock('../../src/hooks/useCurrentUser', () => ({ useCurrentUser: vi.fn() }))

import { useBoards } from '../../src/hooks/useBoards'
import { useCreateBoard } from '../../src/hooks/useCreateBoard'
import { useDeleteBoard } from '../../src/hooks/useDeleteBoard'
import { useCurrentUser } from '../../src/hooks/useCurrentUser'

const mockUseBoards = vi.mocked(useBoards)
const mockUseCreateBoard = vi.mocked(useCreateBoard)
const mockUseDeleteBoard = vi.mocked(useDeleteBoard)
const mockUseCurrentUser = vi.mocked(useCurrentUser)

const idleCreateBoard = {
  mutate: vi.fn(),
  mutateAsync: vi.fn(),
  isPending: false,
  isIdle: true,
  isSuccess: false,
  isError: false,
  isPaused: false,
  data: undefined,
  error: null,
  variables: undefined,
  context: undefined,
  failureCount: 0,
  failureReason: null,
  status: 'idle' as const,
  submittedAt: 0,
  reset: vi.fn(),
} satisfies UseMutationResult<BoardSummary, ApiError, { name: string }>

const idleDeleteBoard = {
  mutate: vi.fn(),
  mutateAsync: vi.fn(),
  isPending: false,
  isIdle: true,
  isSuccess: false,
  isError: false,
  isPaused: false,
  data: undefined,
  error: null,
  variables: undefined,
  context: undefined,
  failureCount: 0,
  failureReason: null,
  status: 'idle' as const,
  submittedAt: 0,
  reset: vi.fn(),
} satisfies UseMutationResult<void, ApiError, string>

const adminUser = {
  user: {
    id: 'user-1',
    email: 'admin@example.com',
    displayName: 'Admin User',
    systemRole: 'admin' as const,
    registeredAt: '2024-01-01T00:00:00Z',
    lastSignInAt: null,
  },
  isLoading: false,
  isUnauthenticated: false,
  isNotRegistered: false,
  isError: false,
}

const standardUser = {
  ...adminUser,
  user: { ...adminUser.user, systemRole: 'standard' as const },
}

const twoBoards: BoardSummary[] = [
  { id: 'board-1', name: 'Sprint Board', laneCount: 3, cardCount: 10, callerRole: 'Owner' },
  { id: 'board-2', name: 'Backlog', laneCount: 2, cardCount: 5, callerRole: 'Member' },
]

function renderPage() {
  const { Wrapper } = createQueryClientWrapper()

  function RouterWrapper({ children }: { children: ReactNode }) {
    return (
      <Wrapper>
        <MemoryRouter initialEntries={['/']}>{children}</MemoryRouter>
      </Wrapper>
    )
  }

  return render(
    <Routes>
      <Route path="/" element={<BoardListPage />} />
      <Route path="/boards/:id" element={<div>Board Page</div>} />
    </Routes>,
    { wrapper: RouterWrapper }
  )
}

describe('BoardListPage', () => {
  beforeEach(() => {
    mockUseCurrentUser.mockReturnValue(adminUser)
    mockUseBoards.mockReturnValue({
      data: twoBoards,
      isPending: false,
      isSuccess: true,
      isError: false,
      error: null,
    } as ReturnType<typeof useBoards>)
    mockUseCreateBoard.mockReturnValue(idleCreateBoard)
    mockUseDeleteBoard.mockReturnValue(idleDeleteBoard)
  })

  it('renders a level-1 heading — WCAG AA gate', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument()
  })

  it('renders each board name in the list', () => {
    renderPage()
    expect(screen.getByText('Sprint Board')).toBeInTheDocument()
    expect(screen.getByText('Backlog')).toBeInTheDocument()
  })

  it('shows a role badge for each board', () => {
    renderPage()
    expect(screen.getByText(/owner/i)).toBeInTheDocument()
    expect(screen.getByText(/member/i)).toBeInTheDocument()
  })

  it('admin sees Create Board button', () => {
    renderPage()
    expect(screen.getByRole('button', { name: /create board/i })).toBeInTheDocument()
  })

  it('non-admin does not see Create Board button', () => {
    mockUseCurrentUser.mockReturnValue(standardUser)
    renderPage()
    expect(screen.queryByRole('button', { name: /create board/i })).not.toBeInTheDocument()
  })

  it('shows empty state message when there are no boards', () => {
    mockUseBoards.mockReturnValue({
      data: [],
      isPending: false,
      isSuccess: true,
      isError: false,
      error: null,
    } as ReturnType<typeof useBoards>)
    renderPage()
    expect(screen.getByText(/no boards|create your first/i)).toBeInTheDocument()
  })
})
