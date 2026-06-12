import { useState } from 'react'
import {
  Button,
  FluentProvider,
  Spinner,
  Title1,
  makeStyles,
  tokens,
  webLightTheme,
} from '@fluentui/react-components'
import { useParams } from 'react-router-dom'
import { useBoard } from '../hooks/useBoard'
import KanbanBoard from '../components/board/KanbanBoard'
import BoardMembersPanel from '../components/board/BoardMembersPanel'

const useStyles = makeStyles({
  root: {
    padding: tokens.spacingHorizontalXXL,
    overflowX: 'auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  membersPanel: {
    marginTop: tokens.spacingVerticalL,
  },
})

export default function BoardPage() {
  const styles = useStyles()
  const { boardId } = useParams<{ boardId: string }>()
  const { data: board, isPending } = useBoard(boardId ?? '')
  const [membersOpen, setMembersOpen] = useState(false)

  if (isPending) return <Spinner label="Loading board…" />

  if (!board) return null

  return (
    <FluentProvider theme={webLightTheme}>
      <div className={styles.root}>
        <div className={styles.header}>
          <Title1 as="h1">{board.name}</Title1>
          <Button onClick={() => setMembersOpen((o) => !o)} aria-label="Members">
            Members
          </Button>
        </div>

        {membersOpen && (
          <div className={styles.membersPanel}>
            <BoardMembersPanel boardId={board.id} callerRole={board.callerRole} />
          </div>
        )}

        <KanbanBoard board={board} />
      </div>
    </FluentProvider>
  )
}
