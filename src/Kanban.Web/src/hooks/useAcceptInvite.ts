import { useMutation } from '@tanstack/react-query'

interface UserDto {
  id: string
  email: string
  displayName: string
  systemRole: string
  registeredAt: string
  lastSignInAt: string | null
}

export interface AcceptInviteResponse {
  user: UserDto
  boardId: string | null
}

export interface AcceptInviteError {
  status: number
  code?: string
  title?: string
}

async function acceptInvite(token: string): Promise<AcceptInviteResponse> {
  const response = await fetch(`/api/v1/invites/${encodeURIComponent(token)}/accept`, {
    method: 'POST',
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as AcceptInviteError
  }
  return response.json() as Promise<AcceptInviteResponse>
}

export function useAcceptInvite() {
  return useMutation<AcceptInviteResponse, AcceptInviteError, string>({
    mutationFn: acceptInvite,
    retry: false,
  })
}
