import { render, screen } from '@testing-library/react'
import { describe, it, expect, vi } from 'vitest'
import CardItem from '../../src/components/board/CardItem'

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

function renderCardItem(card: Card, callerRole: BoardRole = 'Owner', onEdit = vi.fn()) {
  return render(<CardItem card={card} callerRole={callerRole} onEdit={onEdit} />)
}

const baseCard: Card = {
  id: 'card-1',
  laneId: 'lane-1',
  boardId: 'board-1',
  title: 'Test Card Title',
  description: null,
  dueDate: null,
  position: 1,
  version: 1,
}

describe('CardItem', () => {
  it('renders the card title', () => {
    renderCardItem(baseCard)
    expect(screen.getByText('Test Card Title')).toBeInTheDocument()
  })

  it('does not show description indicator when description is absent', () => {
    renderCardItem(baseCard)
    expect(screen.queryByRole('img', { name: /has description/i })).not.toBeInTheDocument()
  })

  it('shows description indicator when description is present', () => {
    renderCardItem({ ...baseCard, description: 'Some description text' })
    expect(screen.getByRole('img', { name: /has description/i })).toBeInTheDocument()
  })

  it('shows edit button for Owner role', () => {
    renderCardItem(baseCard, 'Owner')
    expect(screen.getByRole('button', { name: /edit card/i })).toBeInTheDocument()
  })

  it('shows edit button for Member role', () => {
    renderCardItem(baseCard, 'Member')
    expect(screen.getByRole('button', { name: /edit card/i })).toBeInTheDocument()
  })

  it('does not render edit button for Viewer role', () => {
    renderCardItem(baseCard, 'Viewer')
    expect(screen.queryByRole('button', { name: /edit card/i })).not.toBeInTheDocument()
  })

  it('edit button invokes onEdit with the card', () => {
    const onEdit = vi.fn()
    renderCardItem(baseCard, 'Owner', onEdit)
    screen.getByRole('button', { name: /edit card/i }).click()
    expect(onEdit).toHaveBeenCalledWith(baseCard)
  })

  it('edit button is keyboard-accessible — not excluded from tab order', () => {
    renderCardItem(baseCard)
    const editBtn = screen.getByRole('button', { name: /edit card/i })
    expect(editBtn).not.toHaveAttribute('tabindex', '-1')
  })

  it('card container is reachable as a list item', () => {
    renderCardItem(baseCard)
    expect(screen.getByRole('article')).toBeInTheDocument()
  })
})
