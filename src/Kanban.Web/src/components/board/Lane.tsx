import { Button, Title2, makeStyles, tokens } from '@fluentui/react-components'
import type { Lane as LaneData } from '../../hooks/useBoard'
import type { BoardRole } from '../../hooks/useBoards'

const useStyles = makeStyles({
  lane: {
    minWidth: '260px',
    maxWidth: '320px',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalM,
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  cards: {
    minHeight: '40px',
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  emptySlot: {
    height: '40px',
    borderRadius: tokens.borderRadiusMedium,
    border: `1px dashed ${tokens.colorNeutralStroke2}`,
  },
})

interface LaneProps {
  lane: LaneData
  callerRole: BoardRole
  onDelete: (boardId: string, laneId: string) => void
}

export default function Lane({ lane, callerRole, onDelete }: LaneProps) {
  const styles = useStyles()
  const canModify = callerRole === 'Owner' || callerRole === 'Member'

  return (
    <section className={styles.lane} aria-label={`Lane: ${lane.name}`}>
      <div className={styles.header}>
        <Title2 as="h2">{lane.name}</Title2>
        {canModify && (
          <Button
            size="small"
            appearance="subtle"
            aria-label={`Delete lane ${lane.name}`}
            onClick={() => onDelete(lane.boardId, lane.id)}
          >
            Delete
          </Button>
        )}
      </div>
      <div className={styles.cards}>
        {lane.cards.length === 0 && <div className={styles.emptySlot} aria-hidden="true" />}
      </div>
    </section>
  )
}
