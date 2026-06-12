import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { Button, Text, Badge, makeStyles, tokens } from '@fluentui/react-components'
import { NoteRegular } from '@fluentui/react-icons'
import type { Card } from '../../hooks/useBoard'
import type { BoardRole } from '../../hooks/useBoards'

const useStyles = makeStyles({
  card: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalS,
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    gap: tokens.spacingHorizontalS,
    boxShadow: tokens.shadow2,
    cursor: 'grab',
    touchAction: 'none',
  },
  cardGhost: {
    opacity: 0.4,
  },
  cardViewer: {
    cursor: 'default',
  },
  body: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
    flex: 1,
    minWidth: 0,
  },
  meta: {
    display: 'flex',
    alignItems: 'center',
    gap: tokens.spacingHorizontalXS,
  },
})

interface CardItemProps {
  card: Card
  callerRole: BoardRole
  dragInstructionsId?: string
  onEdit: (card: Card) => void
  onDelete: (card: Card) => void
}

export default function CardItem({
  card,
  callerRole,
  dragInstructionsId,
  onEdit,
  onDelete,
}: CardItemProps) {
  const styles = useStyles()
  const canModify = callerRole === 'Owner' || callerRole === 'Member'

  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: card.id,
    disabled: !canModify,
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition: transition ?? undefined,
  }

  const describedBy = dragInstructionsId ? { 'aria-describedby': dragInstructionsId } : {}

  return (
    <article
      ref={setNodeRef}
      className={`${styles.card}${isDragging ? ` ${styles.cardGhost}` : ''}${!canModify ? ` ${styles.cardViewer}` : ''}`}
      style={style}
      {...(canModify ? attributes : {})}
      {...(canModify ? listeners : {})}
      {...describedBy}
      {...(canModify ? { 'data-drag-handle': '' } : {})}
    >
      <div className={styles.body}>
        <Text>{card.title}</Text>
        <div className={styles.meta}>
          {card.dueDate && (
            <Badge appearance="outline" size="small">
              {card.dueDate}
            </Badge>
          )}
          {card.description && (
            <span role="img" aria-label="has description">
              <NoteRegular fontSize={14} aria-hidden="true" />
            </span>
          )}
        </div>
      </div>
      {canModify && (
        <>
          <Button
            size="small"
            appearance="subtle"
            aria-label={`Edit card ${card.title}`}
            onClick={() => onEdit(card)}
          >
            Edit
          </Button>
          <Button
            size="small"
            appearance="subtle"
            aria-label={`Delete card ${card.title}`}
            onClick={() => onDelete(card)}
          >
            Delete card
          </Button>
        </>
      )}
    </article>
  )
}
