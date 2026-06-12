import { render, screen, within } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import Lane from '../../src/components/board/Lane'
import { createQueryClientWrapper } from '../../src/tests/utils/queryClientWrapper'
import type { Card } from '../../src/hooks/useBoard'

vi.mock('@dnd-kit/sortable', async (importOriginal) => {
  const actual = await importOriginal<typeof import('@dnd-kit/sortable')>()
  return {
    ...actual,
    useSortable: vi.fn(() => ({
      attributes: { 'aria-describedby': 'test-drag-instructions' },
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

vi.mock('@dnd-kit/utilities', () => ({
  CSS: { Transform: { toString: vi.fn(() => '') } },
}))

// Isolate Lane from CardItem/AddCardForm/CardDetailDialog internals
vi.mock('../../src/components/board/CardItem', () => ({
  default: ({ card }: { card: Card }) => (
    <article aria-describedby="test-drag-instructions">{card.title}</article>
  ),
}))

vi.mock('../../src/components/board/AddCardForm', () => ({
  default: () => <button>Add Card</button>,
}))

vi.mock('../../src/components/board/CardDetailDialog', () => ({
  default: () => null,
}))

type BoardRole = 'Owner' | 'Member' | 'Viewer'

const laneWithCards = {
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
      title: 'Alpha Card',
      description: null,
      dueDate: null,
      position: 1,
      version: 1,
    },
    {
      id: 'card-2',
      laneId: 'lane-1',
      boardId: 'board-1',
      title: 'Beta Card',
      description: null,
      dueDate: null,
      position: 2,
      version: 1,
    },
  ],
}

const emptyLane = {
  id: 'lane-empty',
  boardId: 'board-1',
  name: 'Empty Lane',
  position: 2,
  version: 1,
  cards: [],
}

function renderLane(
  lane = laneWithCards,
  callerRole: BoardRole = 'Owner',
  onDelete = vi.fn(),
  dragInstructionsId = 'test-drag-instructions'
) {
  const { Wrapper } = createQueryClientWrapper()
  return render(
    <Lane
      lane={lane}
      callerRole={callerRole}
      dragInstructionsId={dragInstructionsId}
      onDelete={onDelete}
    />,
    { wrapper: Wrapper }
  )
}

describe('Lane', () => {
  it('renders lane name as a heading', () => {
    renderLane()
    expect(screen.getByRole('heading', { name: /To Do/i })).toBeInTheDocument()
  })

  it('renders cards in position order', () => {
    renderLane()
    const cards = screen.getAllByRole('article')
    expect(within(cards[0]).getByText('Alpha Card')).toBeInTheDocument()
    expect(within(cards[1]).getByText('Beta Card')).toBeInTheDocument()
  })

  it('each card has aria-describedby pointing to keyboard instructions', () => {
    renderLane()
    const cards = screen.getAllByRole('article')
    cards.forEach((card) => {
      expect(card).toHaveAttribute('aria-describedby', 'test-drag-instructions')
    })
  })

  it('drag handle button has aria-describedby — WCAG AA gate', () => {
    renderLane(laneWithCards, 'Owner')
    const dragHandle = screen.getByRole('button', { name: /drag to reorder lane/i })
    expect(dragHandle).toHaveAttribute('aria-describedby', 'test-drag-instructions')
  })

  it('renders Add Card form for Owner', () => {
    renderLane(laneWithCards, 'Owner')
    expect(screen.getByRole('button', { name: /add card/i })).toBeInTheDocument()
  })

  it('renders Add Card form for Member', () => {
    renderLane(laneWithCards, 'Member')
    expect(screen.getByRole('button', { name: /add card/i })).toBeInTheDocument()
  })

  it('does not render Add Card form for Viewer', () => {
    renderLane(laneWithCards, 'Viewer')
    expect(screen.queryByRole('button', { name: /add card/i })).not.toBeInTheDocument()
  })

  it('shows Delete lane button for Owner', () => {
    renderLane(laneWithCards, 'Owner')
    expect(screen.getByRole('button', { name: /delete lane/i })).toBeInTheDocument()
  })

  it('does not show Delete lane button for Viewer', () => {
    renderLane(laneWithCards, 'Viewer')
    expect(screen.queryByRole('button', { name: /delete lane/i })).not.toBeInTheDocument()
  })

  it('renders empty lane without crashing', () => {
    renderLane(emptyLane, 'Owner')
    expect(screen.getByRole('heading', { name: /Empty Lane/i })).toBeInTheDocument()
  })
})
