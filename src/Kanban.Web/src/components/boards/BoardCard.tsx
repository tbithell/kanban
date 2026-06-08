import {
  Badge,
  Body1,
  Button,
  Card,
  CardHeader,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { useNavigate } from 'react-router-dom'
import type { BoardSummary } from '../../hooks/useBoards'

const useStyles = makeStyles({
  card: {
    cursor: 'pointer',
    '&:hover': { boxShadow: tokens.shadow8 },
  },
  meta: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
    marginTop: tokens.spacingVerticalXS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
})

interface BoardCardProps {
  board: BoardSummary
  onDelete: (boardId: string) => void
  canDelete: boolean
}

export default function BoardCard({ board, onDelete, canDelete }: BoardCardProps) {
  const styles = useStyles()
  const navigate = useNavigate()

  return (
    <Card
      className={styles.card}
      onClick={() => navigate(`/boards/${board.id}`)}
      aria-label={`Open board ${board.name}`}
    >
      <CardHeader
        header={<span>{board.name}</span>}
        action={
          <div className={styles.actions}>
            <Badge appearance="filled" color="informative">
              {board.callerRole}
            </Badge>
            {canDelete && (
              <Button
                size="small"
                appearance="subtle"
                aria-label={`Delete board ${board.name}`}
                onClick={(e) => {
                  e.stopPropagation()
                  onDelete(board.id)
                }}
              >
                Delete
              </Button>
            )}
          </div>
        }
      />
      <Body1>
        {board.laneCount} lane{board.laneCount !== 1 ? 's' : ''} · {board.cardCount} card
        {board.cardCount !== 1 ? 's' : ''}
      </Body1>
    </Card>
  )
}
