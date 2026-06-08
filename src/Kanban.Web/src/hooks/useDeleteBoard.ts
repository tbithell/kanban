import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoards'

async function deleteBoard(boardId: string): Promise<void> {
  const response = await fetch(`/api/v1/boards/${boardId}`, { method: 'DELETE' })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
}

export function useDeleteBoard() {
  const queryClient = useQueryClient()
  return useMutation<void, ApiError, string>({
    mutationFn: deleteBoard,
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['boards'] }),
    retry: false,
  })
}
