import { useState } from 'react'
import {
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  Field,
  Input,
  MessageBar,
  Textarea,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import type { Card } from '../../hooks/useBoard'
import { useUpdateCard } from '../../hooks/useUpdateCard'
import { useDeleteCard } from '../../hooks/useDeleteCard'

const useStyles = makeStyles({
  fields: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
})

interface CardDetailDialogProps {
  card: Card
  open: boolean
  onClose: () => void
  initialConfirmDelete?: boolean
}

export default function CardDetailDialog({
  card,
  open,
  onClose,
  initialConfirmDelete,
}: CardDetailDialogProps) {
  const styles = useStyles()
  const [title, setTitle] = useState(card.title)
  const [description, setDescription] = useState(card.description ?? '')
  const [dueDate, setDueDate] = useState(card.dueDate ?? '')
  const [clearDueDate, setClearDueDate] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(initialConfirmDelete ?? false)

  const updateCard = useUpdateCard()
  const deleteCard = useDeleteCard()

  const handleSave = () => {
    updateCard.mutate(
      {
        boardId: card.boardId,
        cardId: card.id,
        title,
        description: description || null,
        clearDueDate,
        dueDate: clearDueDate ? null : dueDate || null,
      },
      { onSuccess: onClose }
    )
  }

  const handleDelete = () => {
    deleteCard.mutate({ boardId: card.boardId, cardId: card.id }, { onSuccess: onClose })
  }

  const isMutating = updateCard.isPending || deleteCard.isPending
  const hasError = updateCard.isError || deleteCard.isError

  if (confirmDelete) {
    return (
      <Dialog
        open={open}
        onOpenChange={(_, d) => {
          if (!d.open) onClose()
        }}
      >
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Delete card?</DialogTitle>
            <DialogContent>
              This will permanently delete "{card.title}". This action cannot be undone.
            </DialogContent>
            <DialogActions>
              <Button appearance="primary" onClick={handleDelete} disabled={isMutating}>
                Confirm
              </Button>
              <Button appearance="subtle" onClick={() => setConfirmDelete(false)}>
                Cancel
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>
    )
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(_, d) => {
        if (!d.open) onClose()
      }}
    >
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Edit card</DialogTitle>
          <DialogContent>
            {hasError && (
              <MessageBar role="alert" intent="error">
                Failed to save changes. Please try again.
              </MessageBar>
            )}
            <div className={styles.fields}>
              <Field label="Title" required>
                <Input aria-label="Title" value={title} onChange={(_, d) => setTitle(d.value)} />
              </Field>
              <Field label="Description">
                <Textarea
                  aria-label="Description"
                  value={description}
                  onChange={(_, d) => setDescription(d.value)}
                />
              </Field>
              <Field label="Due date">
                <Input
                  aria-label="Due date"
                  type="date"
                  value={clearDueDate ? '' : dueDate}
                  onChange={(_, d) => setDueDate(d.value)}
                  disabled={clearDueDate}
                />
              </Field>
              <Checkbox
                label="Clear due date"
                checked={clearDueDate}
                onChange={(_, d) => setClearDueDate(!!d.checked)}
              />
            </div>
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={handleSave} disabled={isMutating}>
              Save
            </Button>
            <Button
              appearance="outline"
              onClick={() => setConfirmDelete(true)}
              disabled={isMutating}
            >
              Delete card
            </Button>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="subtle">Cancel</Button>
            </DialogTrigger>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
