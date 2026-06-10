import { useState, useEffect, useRef } from 'react'
import { Button, Field, Input, MessageBar, makeStyles, tokens } from '@fluentui/react-components'
import { useCreateCard } from '../../hooks/useCreateCard'

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginTop: tokens.spacingVerticalS,
  },
  actions: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
})

interface AddCardFormProps {
  boardId: string
  laneId: string
}

export default function AddCardForm({ boardId, laneId }: AddCardFormProps) {
  const styles = useStyles()
  const [open, setOpen] = useState(false)
  const [title, setTitle] = useState('')
  const [titleError, setTitleError] = useState(false)
  const createCard = useCreateCard()
  const inputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (open) inputRef.current?.focus()
  }, [open])

  const handleSubmit = () => {
    if (!title.trim()) {
      setTitleError(true)
      return
    }
    setTitleError(false)
    createCard.mutate(
      { boardId, laneId, title: title.trim() },
      {
        onSuccess: () => {
          setTitle('')
          setOpen(false)
        },
      }
    )
  }

  if (!open) {
    return (
      <Button appearance="subtle" size="small" onClick={() => setOpen(true)} aria-label="Add card">
        + Add card
      </Button>
    )
  }

  return (
    <div className={styles.root}>
      {(titleError || createCard.isError) && (
        <MessageBar role="alert" intent="error">
          {titleError ? 'Card title is required.' : 'Failed to add card. Please try again.'}
        </MessageBar>
      )}
      <Field label="Card title">
        <Input
          ref={inputRef}
          aria-label="Card title"
          value={title}
          onChange={(_, d) => setTitle(d.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') handleSubmit()
            if (e.key === 'Escape') setOpen(false)
          }}
        />
      </Field>
      <div className={styles.actions}>
        <Button
          appearance="primary"
          size="small"
          onClick={handleSubmit}
          disabled={createCard.isPending}
        >
          Add
        </Button>
        <Button
          appearance="subtle"
          size="small"
          onClick={() => {
            setOpen(false)
            setTitle('')
          }}
        >
          Cancel
        </Button>
      </div>
    </div>
  )
}
