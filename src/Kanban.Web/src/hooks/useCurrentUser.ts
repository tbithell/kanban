import { useQuery } from '@tanstack/react-query'

export interface CurrentUser {
  id: string
  email: string
  displayName: string
  systemRole: 'admin' | 'standard'
  registeredAt: string
  lastSignInAt: string | null
}

class HttpError extends Error {
  readonly status: number
  constructor(status: number) {
    super(`HTTP ${status}`)
    this.name = 'HttpError'
    this.status = status
  }
}

async function fetchCurrentUser(): Promise<CurrentUser> {
  const response = await fetch('/api/v1/auth/me')
  if (!response.ok) throw new HttpError(response.status)
  return response.json() as Promise<CurrentUser>
}

export interface UseCurrentUserResult {
  user: CurrentUser | undefined
  isLoading: boolean
  isUnauthenticated: boolean
  isNotRegistered: boolean
  isError: boolean
}

export function useCurrentUser(): UseCurrentUserResult {
  const { data, isPending, isError, error } = useQuery({
    queryKey: ['currentUser'],
    queryFn: fetchCurrentUser,
    retry: false,
  })

  const isUnauthenticated = isError && error instanceof HttpError && error.status === 401
  const isNotRegistered = isError && error instanceof HttpError && error.status === 403

  return {
    user: data,
    isLoading: isPending,
    isUnauthenticated,
    isNotRegistered,
    isError,
  }
}
