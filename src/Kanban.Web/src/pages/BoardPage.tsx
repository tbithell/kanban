import {
  FluentProvider,
  Spinner,
  Title1,
  makeStyles,
  tokens,
  webLightTheme,
} from '@fluentui/react-components'
import { useParams } from 'react-router-dom'
import { useBoard } from '../hooks/useBoard'
import { useCurrentUser } from '../hooks/useCurrentUser'
import { useDeleteLane } from '../hooks/useDeleteLane'
import Lane from '../components/board/Lane'
import AddLaneForm from '../components/board/AddLaneForm'

const useStyles = makeStyles({
  root: {
    padding: tokens.spacingHorizontalXXL,
    overflowX: 'auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
  },
  lanes: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-start',
    overflowX: 'auto',
    paddingBottom: tokens.spacingVerticalM,
  },
})

export default function BoardPage() {
  const styles = useStyles()
  const { boardId } = useParams<{ boardId: string }>()
  const { data: board, isPending } = useBoard(boardId ?? '')
  const { user } = useCurrentUser()
  const deleteLaneMutation = useDeleteLane()

  const handleDeleteLane = (bId: string, laneId: string) => {
    deleteLaneMutation.mutate({ boardId: bId, laneId })
  }

  if (isPending) return <Spinner label="Loading board…" />

  if (!board) return null

  const sortedLanes = [...board.lanes].sort((a, b) => a.position - b.position)

  return (
    <FluentProvider theme={webLightTheme}>
      <div className={styles.root}>
        <div className={styles.header}>
          <Title1 as="h1">{board.name}</Title1>
        </div>
        <div className={styles.lanes}>
          {sortedLanes.map((lane) => (
            <Lane
              key={lane.id}
              lane={lane}
              callerRole={board.callerRole}
              onDelete={handleDeleteLane}
            />
          ))}
        </div>
        {(board.callerRole === 'Owner' ||
          board.callerRole === 'Member' ||
          user?.systemRole === 'admin') && <AddLaneForm boardId={board.id} />}
      </div>
    </FluentProvider>
  )
}
