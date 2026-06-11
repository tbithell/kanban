import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoards'
import type { BoardDetail, Card, Lane } from './useBoard'

export interface MoveCardVars {
  boardId: string
  cardId: string
  targetLaneId: string
  targetPosition: number
  expectedVersion: number
}

async function moveCard(vars: MoveCardVars): Promise<Card> {
  const response = await fetch(`/api/v1/boards/${vars.boardId}/cards/${vars.cardId}/move`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      targetLaneId: vars.targetLaneId,
      targetPosition: vars.targetPosition,
      expectedVersion: vars.expectedVersion,
    }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
  return response.json() as Promise<Card>
}

export function useMoveCard() {
  const queryClient = useQueryClient()

  return useMutation<Card, ApiError, MoveCardVars, { previousBoard: BoardDetail | undefined }>({
    mutationFn: moveCard,

    onMutate: async (vars) => {
      await queryClient.cancelQueries({ queryKey: ['boards', vars.boardId] })
      const previousBoard = queryClient.getQueryData<BoardDetail>(['boards', vars.boardId])

      queryClient.setQueryData<BoardDetail>(['boards', vars.boardId], (old) => {
        if (!old) return old
        const sourceCard = old.lanes.flatMap((l) => l.cards).find((c) => c.id === vars.cardId)
        if (!sourceCard) return old

        const updatedLanes = old.lanes.map((lane): Lane => {
          if (lane.id === sourceCard.laneId && lane.id !== vars.targetLaneId) {
            // Remove from source lane and compact positions
            const remaining = lane.cards
              .filter((c) => c.id !== vars.cardId)
              .sort((a, b) => a.position - b.position)
              .map((c, i) => ({ ...c, position: i + 1 }))
            return { ...lane, cards: remaining }
          }
          if (lane.id === vars.targetLaneId) {
            // Insert into target lane at target position
            const withoutCard = lane.cards.filter((c) => c.id !== vars.cardId)
            const movedCard: Card = {
              ...sourceCard,
              laneId: vars.targetLaneId,
              position: vars.targetPosition,
            }
            const inserted = [
              ...withoutCard.filter((c) => c.position < vars.targetPosition),
              movedCard,
              ...withoutCard
                .filter((c) => c.position >= vars.targetPosition)
                .map((c) => ({ ...c, position: c.position + 1 })),
            ]
            return { ...lane, cards: inserted }
          }
          return lane
        })
        return { ...old, lanes: updatedLanes }
      })

      return { previousBoard }
    },

    onError: (_error, vars, context) => {
      if (context?.previousBoard) {
        queryClient.setQueryData(['boards', vars.boardId], context.previousBoard)
      }
    },

    onSettled: (_data, _error, vars) => {
      queryClient.invalidateQueries({ queryKey: ['boards', vars.boardId] })
    },

    retry: false,
  })
}
