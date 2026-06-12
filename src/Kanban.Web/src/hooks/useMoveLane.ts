import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoards'
import type { BoardDetail, Lane } from './useBoard'

export interface MoveLaneVars {
  boardId: string
  laneId: string
  targetPosition: number
  expectedVersion: number
}

async function moveLane(vars: MoveLaneVars): Promise<Lane> {
  const response = await fetch(`/api/v1/boards/${vars.boardId}/lanes/${vars.laneId}/move`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      targetPosition: vars.targetPosition,
      expectedVersion: vars.expectedVersion,
    }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
  return response.json() as Promise<Lane>
}

export function useMoveLane() {
  const queryClient = useQueryClient()

  return useMutation<Lane, ApiError, MoveLaneVars, { previousBoard: BoardDetail | undefined }>({
    mutationFn: moveLane,

    onMutate: async (vars) => {
      await queryClient.cancelQueries({ queryKey: ['boards', vars.boardId] })
      const previousBoard = queryClient.getQueryData<BoardDetail>(['boards', vars.boardId])

      queryClient.setQueryData<BoardDetail>(['boards', vars.boardId], (old) => {
        if (!old) return old
        const movingLane = old.lanes.find((l) => l.id === vars.laneId)
        if (!movingLane) return old
        const fromPos = movingLane.position
        const toPos = vars.targetPosition

        const reordered = old.lanes
          .map((lane): Lane => {
            if (lane.id === vars.laneId) return { ...lane, position: toPos }
            if (fromPos < toPos && lane.position > fromPos && lane.position <= toPos) {
              return { ...lane, position: lane.position - 1 }
            }
            if (fromPos > toPos && lane.position >= toPos && lane.position < fromPos) {
              return { ...lane, position: lane.position + 1 }
            }
            return lane
          })
          .sort((a, b) => a.position - b.position)

        return { ...old, lanes: reordered }
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
