import { useMutation } from '@tanstack/react-query'

export interface IssueInviteResponse {
  token: string
  redemptionLink: string
  expiresAt: string
}

export interface IssueInviteError {
  status: number
  code?: string
  title?: string
}

async function issueInvite(email: string): Promise<IssueInviteResponse> {
  const response = await fetch('/api/v1/invites', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as IssueInviteError
  }
  return response.json() as Promise<IssueInviteResponse>
}

export function useIssueInvite() {
  return useMutation({
    mutationFn: issueInvite,
    retry: false,
  })
}
