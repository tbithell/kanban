import { useMutation } from '@tanstack/react-query'

export interface AcceptInviteResponse {
  id: string
  email: string
  displayName: string
  systemRole: string
  registeredAt: string
  lastSignInAt: string | null
}

export interface AcceptInviteError {
  status: number
  code?: string
  title?: string
}

async function acceptInvite(token: string): Promise<AcceptInviteResponse> {
  void token
  throw new Error('useAcceptInvite not yet implemented')
}

export function useAcceptInvite() {
  return useMutation<AcceptInviteResponse, AcceptInviteError, string>({
    mutationFn: acceptInvite,
    retry: false,
  })
}
