import { useState } from 'react'
import { useSortable, SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
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
    touchAction: 'none',
  },
  laneGhost: {
    opacity: 0.4,
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
  dragInstructionsId?: string
  onDelete: (boardId: string, laneId: string) => void
}

export default function Lane({ lane, callerRole, dragInstructionsId, onDelete }: LaneProps) {
  const styles = useStyles()
  const canModify = callerRole === 'Owner' || callerRole === 'Member'
  const [editingCard, setEditingCard] = useState<Card | null>(null)
  const [editDialogOpen, setEditDialogOpen] = useState(false)
  const [deletingCard, setDeletingCard] = useState<Card | null>(null)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)

  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: lane.id,
    disabled: !canModify,
  })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition: transition ?? undefined,
  }

  const sortedCards = [...lane.cards].sort((a, b) => a.position - b.position)
  const cardIds = sortedCards.map((c) => c.id)

  const openEdit = (card: Card) => {
    setEditingCard(card)
    setEditDialogOpen(true)
  }

  const openDelete = (card: Card) => {
    setDeletingCard(card)
    setDeleteDialogOpen(true)
  }

  return (
    <section
      ref={setNodeRef}
      className={`${styles.lane}${isDragging ? ` ${styles.laneGhost}` : ''}`}
      style={style}
      aria-label={`Lane: ${lane.name}`}
      {...attributes}
    >
      <div className={styles.header}>
        <Title2
          as="h2"
          aria-label={`Lane: ${lane.name}`}
          {...(canModify ? listeners : {})}
          style={{ cursor: canModify ? 'grab' : 'default' }}
        >
          {lane.name}
        </Title2>
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
      <SortableContext items={cardIds} strategy={verticalListSortingStrategy}>
        <div className={styles.cards}>
          {sortedCards.length === 0 && !canModify && (
            <div className={styles.emptySlot} aria-hidden="true" />
          )}
          {sortedCards.map((card) => (
            <CardItem
              key={card.id}
              card={card}
              callerRole={callerRole}
              dragInstructionsId={dragInstructionsId}
              onEdit={openEdit}
              onDelete={openDelete}
            />
          ))}
        </div>
      </SortableContext>
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
