import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import type { ReactNode } from 'react'
import BoardPage from '../../src/pages/BoardPage'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'

type BoardRole = 'Owner' | 'Member' | 'Viewer'

interface Lane {
  id: string
  boardId: string
  name: string
  position: number
  version: number
  cards: unknown[]
}

interface BoardDetail {
  id: string
  name: string
  createdByUserId: string
  createdAt: string
  callerRole: BoardRole
  lanes: Lane[]
}

// Isolate BoardPage from DnD/KanbanBoard internals — KanbanBoard tests own those concerns.
vi.mock('../../src/components/board/KanbanBoard', () => ({
  default: ({ board }: { board: BoardDetail }) => (
    <section aria-label="Kanban board">
      {[...board.lanes]
        .sort((a, b) => a.position - b.position)
        .map((l) => (
          <h2 key={l.id}>{l.name}</h2>
        ))}
      <button>Add Lane</button>
    </section>
  ),
}))

vi.mock('../../src/hooks/useBoard', () => ({ useBoard: vi.fn() }))

import { useBoard } from '../../src/hooks/useBoard'

const mockUseBoard = vi.mocked(useBoard)

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
    mockUseBoard.mockReturnValue({
      data: boardWithThreeLanes,
      isPending: false,
      isSuccess: true,
      isError: false,
      error: null,
    } as ReturnType<typeof useBoard>)
  })

  it('renders a level-1 heading with the board name — WCAG AA gate', () => {
    renderPage()
    expect(screen.getByRole('heading', { level: 1, name: /My Sprint Board/i })).toBeInTheDocument()
  })

  it('delegates lane rendering to KanbanBoard', () => {
    renderPage()
    expect(screen.getByRole('region', { name: /kanban board/i })).toBeInTheDocument()
    const laneHeadings = screen.getAllByRole('heading', { name: /To Do|In Progress|Done/i })
    expect(laneHeadings[0]).toHaveTextContent('To Do')
    expect(laneHeadings[1]).toHaveTextContent('In Progress')
    expect(laneHeadings[2]).toHaveTextContent('Done')
  })

  it('shows Members button for Owner role', () => {
    renderPage()
    expect(screen.getByRole('button', { name: /members/i })).toBeInTheDocument()
  })

  it('shows Members button for Member role', () => {
    mockUseBoard.mockReturnValue({
      data: { ...boardWithThreeLanes, callerRole: 'Member' },
      isPending: false,
      isSuccess: true,
      isError: false,
      error: null,
    } as ReturnType<typeof useBoard>)
    renderPage()
    expect(screen.getByRole('button', { name: /members/i })).toBeInTheDocument()
  })

  it('shows Members button for Viewer role', () => {
    mockUseBoard.mockReturnValue({
      data: { ...boardWithThreeLanes, callerRole: 'Viewer' },
      isPending: false,
      isSuccess: true,
      isError: false,
      error: null,
    } as ReturnType<typeof useBoard>)
    renderPage()
    expect(screen.getByRole('button', { name: /members/i })).toBeInTheDocument()
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
