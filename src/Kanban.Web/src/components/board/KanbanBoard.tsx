import { useId, useState } from 'react'
import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  closestCenter,
} from '@dnd-kit/core'
import type { DragEndEvent, DragStartEvent } from '@dnd-kit/core'
import {
  SortableContext,
  horizontalListSortingStrategy,
  sortableKeyboardCoordinates,
} from '@dnd-kit/sortable'
import {
  makeStyles,
  tokens,
  Toast,
  ToastBody,
  ToastTitle,
  Toaster,
  useToastController,
} from '@fluentui/react-components'
import type { BoardDetail, Card, Lane as LaneData } from '../../hooks/useBoard'
import { useMoveCard } from '../../hooks/useMoveCard'
import { useMoveLane } from '../../hooks/useMoveLane'
import Lane from './Lane'
import CardDragPreview from './CardDragPreview'
import LaneDragPreview from './LaneDragPreview'
import AddLaneForm from './AddLaneForm'
import { useDeleteLane } from '../../hooks/useDeleteLane'

const useStyles = makeStyles({
  lanes: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
    alignItems: 'flex-start',
    overflowX: 'auto',
    paddingBottom: tokens.spacingVerticalM,
  },
  instructions: {
    position: 'absolute',
    width: '1px',
    height: '1px',
    padding: '0',
    margin: '-1px',
    overflow: 'hidden',
    clip: 'rect(0,0,0,0)',
    whiteSpace: 'nowrap',
    border: '0',
  },
})

interface KanbanBoardProps {
  board: BoardDetail
}

type ActiveItem = { type: 'card'; item: Card } | { type: 'lane'; item: LaneData } | null

export default function KanbanBoard({ board }: KanbanBoardProps) {
  const styles = useStyles()
  const instructionsId = useId()
  const toasterId = useId()
  const { dispatchToast } = useToastController(toasterId)
  const [activeItem, setActiveItem] = useState<ActiveItem>(null)

  const canModify = board.callerRole === 'Owner' || board.callerRole === 'Member'

  const moveCard = useMoveCard()
  const moveLane = useMoveLane()
  const deleteLane = useDeleteLane()

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  )

  const sortedLanes = [...board.lanes].sort((a, b) => a.position - b.position)
  const laneIds = sortedLanes.map((l) => l.id)

  const handleDragStart = ({ active }: DragStartEvent) => {
    const card = board.lanes.flatMap((l) => l.cards).find((c) => c.id === active.id)
    if (card) {
      setActiveItem({ type: 'card', item: card })
      return
    }
    const lane = board.lanes.find((l) => l.id === active.id)
    if (lane) {
      setActiveItem({ type: 'lane', item: lane })
    }
  }

  const handleDragCancel = () => {
    setActiveItem(null)
  }

  const handleDragEnd = ({ active, over }: DragEndEvent) => {
    setActiveItem(null)
    if (!over || active.id === over.id) return

    const draggingCard = board.lanes.flatMap((l) => l.cards).find((c) => c.id === active.id)
    if (draggingCard) {
      // Determine target lane and position
      const overCard = board.lanes.flatMap((l) => l.cards).find((c) => c.id === over.id)
      const overLane = board.lanes.find((l) => l.id === over.id)
      const targetLaneId = overCard?.laneId ?? overLane?.id ?? draggingCard.laneId
      const targetLane = board.lanes.find((l) => l.id === targetLaneId)
      if (!targetLane) return
      const targetPosition = overCard ? overCard.position : targetLane.cards.length + 1
      moveCard.mutate(
        {
          boardId: board.id,
          cardId: draggingCard.id,
          targetLaneId,
          targetPosition,
          expectedVersion: draggingCard.version,
        },
        {
          onError: () =>
            dispatchToast(
              <Toast>
                <ToastTitle>Move failed</ToastTitle>
                <ToastBody>The card could not be moved. Please try again.</ToastBody>
              </Toast>,
              { intent: 'error', pauseOnHover: true }
            ),
        }
      )
      return
    }

    const draggingLane = board.lanes.find((l) => l.id === active.id)
    if (draggingLane) {
      const overLane = board.lanes.find((l) => l.id === over.id)
      if (!overLane) return
      moveLane.mutate(
        {
          boardId: board.id,
          laneId: draggingLane.id,
          targetPosition: overLane.position,
          expectedVersion: draggingLane.version,
        },
        {
          onError: () =>
            dispatchToast(
              <Toast>
                <ToastTitle>Move failed</ToastTitle>
                <ToastBody>The lane could not be moved. Please try again.</ToastBody>
              </Toast>,
              { intent: 'error', pauseOnHover: true }
            ),
        }
      )
    }
  }

  const announcements = {
    onDragStart: ({ active }: DragStartEvent) =>
      `Picked up item ${active.id}. Use arrow keys to move, Space to drop, Escape to cancel.`,
    onDragOver: () => ``,
    onDragEnd: ({ active, over }: DragEndEvent) =>
      over ? `Dropped ${active.id} at position near ${over.id}.` : `Drop cancelled.`,
    onDragCancel: ({ active }) => `Move cancelled for ${active.id}.`,
  }

  return (
    <>
      <Toaster toasterId={toasterId} position="bottom-end" />
      <span id={instructionsId + '-drag-instructions'} className={styles.instructions}>
        Press Space or Enter to start dragging. Use arrow keys to move. Press Space or Enter to
        drop, or Escape to cancel.
      </span>
      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        onDragStart={handleDragStart}
        onDragEnd={handleDragEnd}
        onDragCancel={handleDragCancel}
        accessibility={{ announcements }}
      >
        <SortableContext items={laneIds} strategy={horizontalListSortingStrategy}>
          <div className={styles.lanes}>
            {sortedLanes.map((lane) => (
              <Lane
                key={lane.id}
                lane={lane}
                callerRole={board.callerRole}
                dragInstructionsId={instructionsId + '-drag-instructions'}
                onDelete={(bId, lId) => deleteLane.mutate({ boardId: bId, laneId: lId })}
              />
            ))}
          </div>
        </SortableContext>
        <DragOverlay>
          {activeItem?.type === 'card' && <CardDragPreview card={activeItem.item} />}
          {activeItem?.type === 'lane' && <LaneDragPreview lane={activeItem.item} />}
        </DragOverlay>
      </DndContext>
      {canModify && <AddLaneForm boardId={board.id} />}
    </>
  )
}
