import {
  FluentProvider,
  Spinner,
  Title1,
  makeStyles,
  tokens,
  webLightTheme,
} from '@fluentui/react-components'
import { useBoards } from '../hooks/useBoards'
import { useCurrentUser } from '../hooks/useCurrentUser'
import { useDeleteBoard } from '../hooks/useDeleteBoard'
import BoardCard from '../components/boards/BoardCard'
import CreateBoardDialog from '../components/boards/CreateBoardDialog'

const useStyles = makeStyles({
  root: {
    padding: tokens.spacingHorizontalXXL,
    maxWidth: '960px',
    margin: '0 auto',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalL,
  },
  grid: {
    display: 'grid',
    gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
    gap: tokens.spacingHorizontalM,
  },
  empty: {
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalL,
  },
})

export default function BoardListPage() {
  const styles = useStyles()
  const { data: boards, isPending } = useBoards()
  const { user } = useCurrentUser()
  const deleteBoardMutation = useDeleteBoard()

  const isAdmin = user?.systemRole === 'admin'

  return (
    <FluentProvider theme={webLightTheme}>
      <div className={styles.root}>
        <div className={styles.header}>
          <Title1 as="h1">Boards</Title1>
          {isAdmin && <CreateBoardDialog />}
        </div>

        {isPending && <Spinner label="Loading boards…" />}

        {!isPending && boards?.length === 0 && (
          <p className={styles.empty}>No boards yet. Create your first board to get started.</p>
        )}

        {!isPending && boards && boards.length > 0 && (
          <div className={styles.grid}>
            {boards.map((board) => (
              <BoardCard
                key={board.id}
                board={board}
                canDelete={isAdmin || board.callerRole === 'Owner'}
                onDelete={(id) => deleteBoardMutation.mutate(id)}
              />
            ))}
          </div>
        )}
      </div>
    </FluentProvider>
  )
}
