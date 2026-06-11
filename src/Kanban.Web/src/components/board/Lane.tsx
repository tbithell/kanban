import { useState } from 'react'
import { Button, Title2, makeStyles, tokens } from '@fluentui/react-components'
import type { Card, Lane as LaneData } from '../../hooks/useBoard'
import type { BoardRole } from '../../hooks/useBoards'
import CardItem from './CardItem'
import AddCardForm from './AddCardForm'
import CardDetailDialog from './CardDetailDialog'

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
  const [editingCard, setEditingCard] = useState<Card | null>(null)
  const [editDialogOpen, setEditDialogOpen] = useState(false)
  const [deletingCard, setDeletingCard] = useState<Card | null>(null)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)

  const sortedCards = [...lane.cards].sort((a, b) => a.position - b.position)

  const openEdit = (card: Card) => {
    setEditingCard(card)
    setEditDialogOpen(true)
  }

  const openDelete = (card: Card) => {
    setDeletingCard(card)
    setDeleteDialogOpen(true)
  }

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
        {sortedCards.length === 0 && !canModify && (
          <div className={styles.emptySlot} aria-hidden="true" />
        )}
        {sortedCards.map((card) => (
          <CardItem
            key={card.id}
            card={card}
            callerRole={callerRole}
            onEdit={openEdit}
            onDelete={openDelete}
          />
        ))}
      </div>
      {canModify && <AddCardForm boardId={lane.boardId} laneId={lane.id} />}
      {editingCard && (
        <CardDetailDialog
          key={editingCard.id}
          card={editingCard}
          open={editDialogOpen}
          onClose={() => setEditDialogOpen(false)}
        />
      )}
      {deletingCard && (
        <CardDetailDialog
          key={`delete-${deletingCard.id}`}
          card={deletingCard}
          open={deleteDialogOpen}
          onClose={() => setDeleteDialogOpen(false)}
          initialConfirmDelete={true}
        />
      )}
    </section>
  )
}
