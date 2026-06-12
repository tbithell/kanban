import { useQuery } from '@tanstack/react-query'

export interface BoardMember {
  userId: string
  displayName: string
  role: 'Owner' | 'Member' | 'Viewer'
  joinedAt: string
}

export interface ApiError {
  status: number
  code?: string
  title?: string
}

async function fetchBoardMembers(boardId: string): Promise<BoardMember[]> {
  const response = await fetch(`/api/v1/boards/${encodeURIComponent(boardId)}/members`)
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
  return response.json() as Promise<BoardMember[]>
}

export function useBoardMembers(boardId: string) {
  return useQuery({
    queryKey: ['boards', boardId, 'members'],
    queryFn: () => fetchBoardMembers(boardId),
    enabled: Boolean(boardId),
  })
}
