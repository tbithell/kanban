import { describe, it, expect } from 'vitest'
import { applyCardMoveOptimistic } from '../../src/hooks/useMoveCard'
import type { BoardDetail, Card, Lane } from '../../src/hooks/useBoard'

function makeCard(id: string, position: number, laneId = 'lane-1'): Card {
  return {
    id,
    laneId,
    boardId: 'board-1',
    title: id,
    description: null,
    dueDate: null,
    position,
    version: 1,
  }
}

function makeLane(id: string, cards: Card[], position = 1): Lane {
  return { id, boardId: 'board-1', name: id, position, version: 1, cards }
}

function makeBoard(lanes: Lane[]): BoardDetail {
  return {
    id: 'board-1',
    name: 'Test Board',
    createdByUserId: 'user-1',
    createdAt: '',
    callerRole: 'Owner',
    lanes,
  }
}

describe('applyCardMoveOptimistic', () => {
  describe('same-lane reorder', () => {
    it('produces gapless positions after moving card forward', () => {
      const board = makeBoard([
        makeLane('lane-1', [makeCard('a', 1), makeCard('b', 2), makeCard('c', 3)]),
      ])

      const result = applyCardMoveOptimistic(board, {
        boardId: 'board-1',
        cardId: 'a',
        targetLaneId: 'lane-1',
        targetPosition: 3,
        expectedVersion: 1,
      })

      const cards = result.lanes[0].cards.sort((x, y) => x.position - y.position)
      expect(cards.map((c) => c.position)).toEqual([1, 2, 3])
      expect(cards.find((c) => c.id === 'a')?.position).toBe(3)
    })

    it('produces gapless positions after moving card backward', () => {
      const board = makeBoard([
        makeLane('lane-1', [makeCard('a', 1), makeCard('b', 2), makeCard('c', 3)]),
      ])

      const result = applyCardMoveOptimistic(board, {
        boardId: 'board-1',
        cardId: 'c',
        targetLaneId: 'lane-1',
        targetPosition: 1,
        expectedVersion: 1,
      })

      const cards = result.lanes[0].cards.sort((x, y) => x.position - y.position)
      expect(cards.map((c) => c.position)).toEqual([1, 2, 3])
      expect(cards.find((c) => c.id === 'c')?.position).toBe(1)
    })
  })

  describe('cross-lane move', () => {
    it('removes card from source lane with gapless positions', () => {
      const board = makeBoard([
        makeLane('lane-1', [makeCard('a', 1, 'lane-1'), makeCard('b', 2, 'lane-1')], 1),
        makeLane('lane-2', [makeCard('c', 1, 'lane-2')], 2),
      ])

      const result = applyCardMoveOptimistic(board, {
        boardId: 'board-1',
        cardId: 'a',
        targetLaneId: 'lane-2',
        targetPosition: 1,
        expectedVersion: 1,
      })

      const srcCards = result.lanes.find((l) => l.id === 'lane-1')!.cards
      expect(srcCards).toHaveLength(1)
      expect(srcCards[0].id).toBe('b')
      expect(srcCards[0].position).toBe(1)
    })

    it('inserts card into target lane with gapless positions', () => {
      const board = makeBoard([
        makeLane('lane-1', [makeCard('a', 1, 'lane-1'), makeCard('b', 2, 'lane-1')], 1),
        makeLane('lane-2', [makeCard('c', 1, 'lane-2')], 2),
      ])

      const result = applyCardMoveOptimistic(board, {
        boardId: 'board-1',
        cardId: 'a',
        targetLaneId: 'lane-2',
        targetPosition: 1,
        expectedVersion: 1,
      })

      const dstCards = result.lanes
        .find((l) => l.id === 'lane-2')!
        .cards.sort((x, y) => x.position - y.position)
      expect(dstCards).toHaveLength(2)
      expect(dstCards.map((c) => c.position)).toEqual([1, 2])
      expect(dstCards.find((c) => c.id === 'a')?.position).toBe(1)
    })
  })
})
