import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoards'

async function deleteLane(boardId: string, laneId: string): Promise<void> {
  const response = await fetch(`/api/v1/boards/${boardId}/lanes/${laneId}`, { method: 'DELETE' })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
}

export function useDeleteLane() {
  const queryClient = useQueryClient()
  return useMutation<void, ApiError, { boardId: string; laneId: string }>({
    mutationFn: ({ boardId, laneId }) => deleteLane(boardId, laneId),
    onSettled: (_data, _error, { boardId }) =>
      queryClient.invalidateQueries({ queryKey: ['boards', boardId] }),
    retry: false,
  })
}
