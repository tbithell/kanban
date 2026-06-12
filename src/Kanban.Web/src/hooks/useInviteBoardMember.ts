import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ApiError } from './useBoardMembers'

interface InviteRequest {
  boardId: string
  email: string
  role: 'Owner' | 'Member' | 'Viewer'
}

async function inviteBoardMember({ boardId, email, role }: InviteRequest): Promise<void> {
  const response = await fetch(`/api/v1/boards/${encodeURIComponent(boardId)}/members/invite`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, role }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
}

export function useInviteBoardMember() {
  const queryClient = useQueryClient()
  return useMutation<void, ApiError, InviteRequest>({
    mutationFn: inviteBoardMember,
    onSettled: (_data, _error, { boardId }) =>
      queryClient.invalidateQueries({ queryKey: ['boards', boardId, 'members'] }),
    retry: false,
  })
}
