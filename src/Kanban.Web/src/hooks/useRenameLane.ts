import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoards'

async function renameLane(boardId: string, laneId: string, name: string): Promise<void> {
  const response = await fetch(`/api/v1/boards/${boardId}/lanes/${laneId}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
}

export function useRenameLane() {
  const queryClient = useQueryClient()
  return useMutation<void, ApiError, { boardId: string; laneId: string; name: string }>({
    mutationFn: ({ boardId, laneId, name }) => renameLane(boardId, laneId, name),
    onSettled: (_data, _error, { boardId }) =>
      queryClient.invalidateQueries({ queryKey: ['boards', boardId] }),
    retry: false,
  })
}
