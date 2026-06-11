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
  onEdit: (card: Card) => void
  onDelete: (card: Card) => void
}

export default function CardItem({ card, callerRole, onEdit, onDelete }: CardItemProps) {
  const styles = useStyles()
  const canModify = callerRole === 'Owner' || callerRole === 'Member'

  return (
    <article className={styles.card}>
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
