import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import type { UseMutationResult } from '@tanstack/react-query'
import KanbanBoard from '../../src/components/board/KanbanBoard'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'
import type { Lane as LaneData } from '../../src/hooks/useBoard'

// dnd-kit recommends mocking sensors in RTL tests — actual drag behaviour is
// owned by Playwright e2e (DragDropTests).
vi.mock('@dnd-kit/core', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@dnd-kit/core')>()
  return {
    ...actual,
    useSensor: vi.fn(),
    useSensors: vi.fn(() => []),
    PointerSensor: class MockPointerSensor {},
    KeyboardSensor: class MockKeyboardSensor {},
  }
})

vi.mock('@dnd-kit/sortable', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@dnd-kit/sortable')>()
  return {
    ...actual,
    useSortable: vi.fn(() => ({
      attributes: {},
      listeners: {},
      setNodeRef: vi.fn(),
      transform: null,
      transition: null,
      isDragging: false,
    })),
    SortableContext: ({ children }: { children: React.ReactNode }) => <>{children}</>,
    sortableKeyboardCoordinates: vi.fn(),
  }
})

// Isolate KanbanBoard from Lane/AddLaneForm internals
vi.mock('../../src/components/board/Lane', () => ({
  default: ({ lane }: { lane: LaneData }) => (
    <section aria-label={`Lane: ${lane.name}`}>
      <h2>{lane.name}</h2>
    </section>
  ),
}))

vi.mock('../../src/components/board/AddLaneForm', () => ({
  default: () => <button>Add Lane</button>,
}))

vi.mock('../../src/hooks/useMoveCard', () => ({ useMoveCard: vi.fn() }))
vi.mock('../../src/hooks/useMoveLane', () => ({ useMoveLane: vi.fn() }))
vi.mock('../../src/hooks/useDeleteLane', () => ({ useDeleteLane: vi.fn() }))

import { useMoveCard } from '../../src/hooks/useMoveCard'
import { useMoveLane } from '../../src/hooks/useMoveLane'
import { useDeleteLane } from '../../src/hooks/useDeleteLane'

type BoardRole = 'Owner' | 'Member' | 'Viewer'

interface ApiError {
  status: number
  code?: string
  title?: string
}

interface MoveCardVars {
  boardId: string
  cardId: string
  targetLaneId: string
  targetPosition: number
  expectedVersion: number
}

interface MoveLaneVars {
  boardId: string
  laneId: string
  targetPosition: number
  expectedVersion: number
}

const idleMoveCard = {
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
} satisfies UseMutationResult<unknown, ApiError, MoveCardVars>

const idleMoveLane = {
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
} satisfies UseMutationResult<unknown, ApiError, MoveLaneVars>

const idleDeleteLane = {
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

const mockUseMoveCard = vi.mocked(useMoveCard)
const mockUseMoveLane = vi.mocked(useMoveLane)
const mockUseDeleteLane = vi.mocked(useDeleteLane)

const boardWithCards = {
  id: 'board-1',
  name: 'Sprint Board',
  createdByUserId: 'user-1',
  createdAt: '2024-01-01T00:00:00Z',
  callerRole: 'Owner' as BoardRole,
  lanes: [
    {
      id: 'lane-1',
      boardId: 'board-1',
      name: 'To Do',
      position: 1,
      version: 1,
      cards: [
        {
          id: 'card-1',
          laneId: 'lane-1',
          boardId: 'board-1',
          title: 'First Card',
          description: null,
          dueDate: null,
          position: 1,
          version: 1,
        },
        {
          id: 'card-2',
          laneId: 'lane-1',
          boardId: 'board-1',
          title: 'Second Card',
          description: null,
          dueDate: null,
          position: 2,
          version: 1,
        },
      ],
    },
    {
      id: 'lane-2',
      boardId: 'board-1',
      name: 'Done',
      position: 2,
      version: 1,
      cards: [],
    },
  ],
}

function renderBoard(callerRole: BoardRole = 'Owner') {
  const { Wrapper } = createQueryClientWrapper()
  return render(<KanbanBoard board={{ ...boardWithCards, callerRole }} />, { wrapper: Wrapper })
}

describe('KanbanBoard', () => {
  beforeEach(() => {
    mockUseMoveCard.mockReturnValue(idleMoveCard as ReturnType<typeof useMoveCard>)
    mockUseMoveLane.mockReturnValue(idleMoveLane as ReturnType<typeof useMoveLane>)
    mockUseDeleteLane.mockReturnValue(idleDeleteLane as ReturnType<typeof useDeleteLane>)
  })

  it('renders all lanes', () => {
    renderBoard()
    expect(screen.getByRole('heading', { name: /To Do/i })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /Done/i })).toBeInTheDocument()
  })

  it('provides an accessible announcements region for screen readers', () => {
    renderBoard()
    const liveRegion = document.querySelector('[aria-live]')
    expect(liveRegion).toBeInTheDocument()
  })

  it('renders keyboard drag instructions element', () => {
    renderBoard()
    const instructions = document.querySelector('[id*="drag-instructions"]')
    expect(instructions).toBeInTheDocument()
  })

  it('wires useMoveCard hook', () => {
    renderBoard()
    expect(mockUseMoveCard).toHaveBeenCalled()
  })

  it('wires useMoveLane hook', () => {
    renderBoard()
    expect(mockUseMoveLane).toHaveBeenCalled()
  })

  it('shows Add Lane form for Owner', () => {
    renderBoard('Owner')
    expect(screen.getByRole('button', { name: /add lane/i })).toBeInTheDocument()
  })

  it('shows Add Lane form for Member', () => {
    renderBoard('Member')
    expect(screen.getByRole('button', { name: /add lane/i })).toBeInTheDocument()
  })

  it('does not show Add Lane form for Viewer', () => {
    renderBoard('Viewer')
    expect(screen.queryByRole('button', { name: /add lane/i })).not.toBeInTheDocument()
  })
})
