import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoards'
import type { Lane } from './useBoard'

async function createLane(boardId: string, name: string): Promise<Lane> {
  const response = await fetch(`/api/v1/boards/${boardId}/lanes`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
  return response.json() as Promise<Lane>
}

export function useCreateLane() {
  const queryClient = useQueryClient()
  return useMutation<Lane, ApiError, { boardId: string; name: string }>({
    mutationFn: ({ boardId, name }) => createLane(boardId, name),
    onSettled: (_data, _error, { boardId }) =>
      queryClient.invalidateQueries({ queryKey: ['boards', boardId] }),
    retry: false,
  })
}
