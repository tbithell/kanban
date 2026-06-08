import { useQuery } from '@tanstack/react-query'

export type BoardRole = 'Owner' | 'Member' | 'Viewer'

export interface BoardSummary {
  id: string
  name: string
  laneCount: number
  cardCount: number
  callerRole: BoardRole
}

export interface ApiError {
  status: number
  code?: string
  title?: string
}

async function fetchBoards(): Promise<BoardSummary[]> {
  const response = await fetch('/api/v1/boards')
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
  return response.json() as Promise<BoardSummary[]>
}

export function useBoards() {
  return useQuery({
    queryKey: ['boards'],
    queryFn: fetchBoards,
  })
}
