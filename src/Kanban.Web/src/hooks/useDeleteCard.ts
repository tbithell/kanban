import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoards'

interface DeleteCardVars {
  boardId: string
  cardId: string
}

async function deleteCard(vars: DeleteCardVars): Promise<void> {
  const response = await fetch(`/api/v1/boards/${vars.boardId}/cards/${vars.cardId}`, {
    method: 'DELETE',
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
}

export function useDeleteCard() {
  const queryClient = useQueryClient()
  return useMutation<void, ApiError, DeleteCardVars>({
    mutationFn: deleteCard,
    onSettled: (_data, _error, { boardId }) =>
      queryClient.invalidateQueries({ queryKey: ['boards', boardId] }),
    retry: false,
  })
}
