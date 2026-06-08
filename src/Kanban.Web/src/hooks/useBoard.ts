import { useQuery } from '@tanstack/react-query'
import type { ApiError, BoardRole } from './useBoards'

export interface Card {
  id: string
  laneId: string
  boardId: string
  title: string
  description: string | null
  dueDate: string | null
  position: number
  version: number
}

export interface Lane {
  id: string
  boardId: string
  name: string
  position: number
  version: number
  cards: Card[]
}

export interface BoardDetail {
  id: string
  name: string
  createdByUserId: string
  createdAt: string
  callerRole: BoardRole
  lanes: Lane[]
}

async function fetchBoard(boardId: string): Promise<BoardDetail> {
  const response = await fetch(`/api/v1/boards/${boardId}`)
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw { status: response.status, code: body.code, title: body.title } as ApiError
  }
  return response.json() as Promise<BoardDetail>
}

export function useBoard(boardId: string) {
  return useQuery({
    queryKey: ['boards', boardId],
    queryFn: () => fetchBoard(boardId),
    enabled: Boolean(boardId),
  })
}
