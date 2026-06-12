import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoardMembers'

interface RemoveMemberRequest {
  boardId: string
  userId: string
}

async function removeBoardMember({ boardId, userId }: RemoveMemberRequest): Promise<void> {
  const response = await fetch(
    `/api/v1/boards/${encodeURIComponent(boardId)}/members/${encodeURIComponent(userId)}`,
    { method: 'DELETE' }
  )
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
}

export function useRemoveBoardMember() {
  const queryClient = useQueryClient()
  return useMutation<void, ApiError, RemoveMemberRequest>({
    mutationFn: removeBoardMember,
    onSettled: (_data, _error, { boardId }) =>
      queryClient.invalidateQueries({ queryKey: ['boards', boardId, 'members'] }),
    retry: false,
  })
}
