import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError, BoardSummary } from './useBoards'

async function createBoard(name: string): Promise<BoardSummary> {
  const response = await fetch('/api/v1/boards', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
  return response.json() as Promise<BoardSummary>
}

export function useCreateBoard() {
  const queryClient = useQueryClient()
  return useMutation<BoardSummary, ApiError, { name: string }>({
    mutationFn: ({ name }) => createBoard(name),
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['boards'] }),
    retry: false,
  })
}
