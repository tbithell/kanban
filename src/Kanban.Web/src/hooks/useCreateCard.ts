import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoards'
import type { Card } from './useBoard'

interface CreateCardVars {
  boardId: string
  laneId: string
  title: string
  description?: string | null
  dueDate?: string | null
}

async function createCard(vars: CreateCardVars): Promise<Card> {
  const response = await fetch(`/api/v1/boards/${vars.boardId}/lanes/${vars.laneId}/cards`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      title: vars.title,
      description: vars.description ?? null,
      dueDate: vars.dueDate ?? null,
    }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
  return response.json() as Promise<Card>
}

export function useCreateCard() {
  const queryClient = useQueryClient()
  return useMutation<Card, ApiError, CreateCardVars>({
    mutationFn: createCard,
    onSettled: (_data, _error, { boardId }) =>
      queryClient.invalidateQueries({ queryKey: ['boards', boardId] }),
    retry: false,
  })
}
