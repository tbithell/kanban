import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import type { ReactNode } from 'react'
import type { UseMutationResult } from '@tanstack/react-query'
import BoardPage from '../../src/pages/BoardPage'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'

type BoardRole = 'Owner' | 'Member' | 'Viewer'

interface Card {
  id: string
  laneId: string
  boardId: string
  title: string
  description: string | null
  dueDate: string | null
  position: number
  version: number
}

interface Lane {
  id: string
  boardId: string
  name: string
  position: number
  version: number
  cards: Card[]
}

interface BoardDetail {
  id: string
  name: string
  createdByUserId: string
  createdAt: string
  callerRole: BoardRole
  lanes: Lane[]
}

interface ApiError {
  status: number
  code?: string
  title?: string
}

vi.mock('../../src/hooks/useBoard', () => ({ useBoard: vi.fn() }))
vi.mock('../../src/hooks/useCreateLane', () => ({ useCreateLane: vi.fn() }))
vi.mock('../../src/hooks/useRenameLane', () => ({ useRenameLane: vi.fn() }))
vi.mock('../../src/hooks/useDeleteLane', () => ({ useDeleteLane: vi.fn() }))
vi.mock('../../src/hooks/useCurrentUser', () => ({ useCurrentUser: vi.fn() }))

import { useBoard } from '../../src/hooks/useBoard'
import { useCreateLane } from '../../src/hooks/useCreateLane'
import { useRenameLane } from '../../src/hooks/useRenameLane'
import { useDeleteLane } from '../../src/hooks/useDeleteLane'
import { useCurrentUser } from '../../src/hooks/useCurrentUser'

const mockUseBoard = vi.mocked(useBoard)
const mockUseCreateLane = vi.mocked(useCreateLane)
const mockUseRenameLane = vi.mocked(useRenameLane)
const mockUseDeleteLane = vi.mocked(useDeleteLane)
const mockUseCurrentUser = vi.mocked(useCurrentUser)

const idleCreateLane = {
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
} satisfies UseMutationResult<Lane, ApiError, { boardId: string; name: string }>

const idleRenameMutation = {
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
} satisfies UseMutationResult<void, ApiError, { boardId: string; laneId: string; name: string }>

const idleDeleteMutation = {
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
} satisfies UseMutationResult<void, ApiError, { boardId: string; laneId: string }>

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

const boardWithThreeLanes: BoardDetail = {
  id: 'board-1',
  name: 'My Sprint Board',
  createdByUserId: 'user-1',
  createdAt: '2024-01-01T00:00:00Z',
  callerRole: 'Owner',
  lanes: [
    { id: 'lane-1', boardId: 'board-1', name: 'To Do', position: 1, version: 1, cards: [] },
    { id: 'lane-2', boardId: 'board-1', name: 'In Progress', position: 2, version: 1, cards: [] },
    { id: 'lane-3', boardId: 'board-1', name: 'Done', position: 3, version: 1, cards: [] },
  ],
}

function renderPage(boardId = 'board-1') {
  const { Wrapper } = createQueryClientWrapper()

  function RouterWrapper({ children }: { children: ReactNode }) {
    return (
      <Wrapper>
        <MemoryRouter initialEntries={[`/boards/${boardId}`]}>{children}</MemoryRouter>
      </Wrapper>
    )
  }

  return render(
    <Routes>
      <Route path="/boards/:boardId" element={<BoardPage />} />
    </Routes>,
    { wrapper: RouterWrapper }
  )
}

describe('BoardPage', () => {
  beforeEach(() => {
    mockUseCurrentUser.mockReturnValue(adminUser)
    mockUseBoard.mockReturnValue({
      data: boardWithThreeLanes,
      isPending: false,
      isSuccess: true,
      isError: false,
      error: null,
    } as ReturnType<typeof useBoard>)
    mockUseCreateLane.mockReturnValue(idleCreateLane)
    mockUseRenameLane.mockReturnValue(idleRenameMutation)
    mockUseDeleteLane.mockReturnValue(idleDeleteMutation)
  })

  it('renders a level-1 heading with the board name — WCAG AA gate', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1, name: /My Sprint Board/i })).toBeInTheDocument()
  })

  it('renders lanes in position order', () => {
    renderPage()
    const laneHeadings = screen.getAllByRole('heading', { name: /To Do|In Progress|Done/i })
    expect(laneHeadings[0]).toHaveTextContent('To Do')
    expect(laneHeadings[1]).toHaveTextContent('In Progress')
    expect(laneHeadings[2]).toHaveTextContent('Done')
  })

  it('shows Add Lane affordance', () => {
    renderPage()
    expect(screen.getByRole('button', { name: /add lane/i })).toBeInTheDocument()
  })

  it('does not render a heading when board data is pending', () => {
    mockUseBoard.mockReturnValue({
      data: undefined,
      isPending: true,
      isSuccess: false,
      isError: false,
      error: null,
    } as ReturnType<typeof useBoard>)
    renderPage()
    expect(screen.queryByRole('heading', { level: 1 })).not.toBeInTheDocument()
  })
})
