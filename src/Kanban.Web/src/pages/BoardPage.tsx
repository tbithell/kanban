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
import KanbanBoard from '../components/board/KanbanBoard'

const useStyles = makeStyles({
  root: {
    padding: tokens.spacingHorizontalXXL,
    overflowX: 'auto',
  },
  header: {
    marginBottom: tokens.spacingVerticalL,
  },
})

export default function BoardPage() {
  const styles = useStyles()
  const { boardId } = useParams<{ boardId: string }>()
  const { data: board, isPending } = useBoard(boardId ?? '')

  if (isPending) return <Spinner label="Loading board…" />

  if (!board) return null

  return (
    <FluentProvider theme={webLightTheme}>
      <div className={styles.root}>
        <div className={styles.header}>
          <Title1 as="h1">{board.name}</Title1>
        </div>
        <KanbanBoard board={board} />
      </div>
    </FluentProvider>
  )
}
