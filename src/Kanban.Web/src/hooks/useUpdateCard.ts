import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoards'
import type { Card } from './useBoard'

interface UpdateCardVars {
  boardId: string
  cardId: string
  title?: string | null
  description?: string | null
  clearDueDate: boolean
  dueDate?: string | null
}

async function updateCard(vars: UpdateCardVars): Promise<Card> {
  const response = await fetch(`/api/v1/boards/${vars.boardId}/cards/${vars.cardId}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      title: vars.title ?? null,
      description: vars.description ?? null,
      clearDueDate: vars.clearDueDate,
      dueDate: vars.dueDate ?? null,
    }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
  return response.json() as Promise<Card>
}

export function useUpdateCard() {
  const queryClient = useQueryClient()
  return useMutation<Card, ApiError, UpdateCardVars>({
    mutationFn: updateCard,
    onSettled: (_data, _error, { boardId }) =>
      queryClient.invalidateQueries({ queryKey: ['boards', boardId] }),
    retry: false,
  })
}
