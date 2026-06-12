import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { BoardMember, ApiError } from './useBoardMembers'

interface ChangeRoleRequest {
  boardId: string
  userId: string
  role: 'Owner' | 'Member' | 'Viewer'
}

async function changeMemberRole({
  boardId,
  userId,
  role,
}: ChangeRoleRequest): Promise<BoardMember> {
  const response = await fetch(
    `/api/v1/boards/${encodeURIComponent(boardId)}/members/${encodeURIComponent(userId)}/role`,
    {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ role }),
    }
  )
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
  return response.json() as Promise<BoardMember>
}

export function useChangeMemberRole() {
  const queryClient = useQueryClient()
  return useMutation<BoardMember, ApiError, ChangeRoleRequest>({
    mutationFn: changeMemberRole,
    onSettled: (_data, _error, { boardId }) =>
      queryClient.invalidateQueries({ queryKey: ['boards', boardId, 'members'] }),
    retry: false,
  })
}
