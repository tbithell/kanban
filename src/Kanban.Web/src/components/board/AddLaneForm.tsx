import {
  Button,
  Input,
  MessageBar,
  MessageBarBody,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { useState } from 'react'
import { useCreateLane } from '../../hooks/useCreateLane'

const useStyles = makeStyles({
  form: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalS,
  },
  row: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    alignItems: 'center',
  },
})

interface AddLaneFormProps {
  boardId: string
}

export default function AddLaneForm({ boardId }: AddLaneFormProps) {
  const styles = useStyles()
  const [expanded, setExpanded] = useState(false)
  const [name, setName] = useState('')
  const { mutate, isPending, isError, error, reset } = useCreateLane()

  const handleAdd = () => {
    const trimmed = name.trim()
    if (!trimmed) return
    mutate(
      { boardId, name: trimmed },
      {
        onSuccess: () => {
          setName('')
          setExpanded(false)
        },
      }
    )
  }

  if (!expanded) {
    return (
      <Button
        appearance="primary"
        aria-label="Add lane"
        onClick={() => {
          reset()
          setName('')
          setExpanded(true)
        }}
      >
        Add lane
      </Button>
    )
  }

  return (
    <div className={styles.form}>
      {isError && (
        <MessageBar role="alert" intent="error">
          <MessageBarBody>{error?.title ?? 'Failed to add lane'}</MessageBarBody>
        </MessageBar>
      )}
      <div className={styles.row}>
        <Input
          aria-label="Lane name"
          placeholder="Lane name"
          value={name}
          ref={(el) => el?.focus()}
          onChange={(_, d) => setName(d.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') handleAdd()
          }}
        />
        <Button appearance="primary" disabled={!name.trim() || isPending} onClick={handleAdd}>
          Add
        </Button>
        <Button appearance="secondary" onClick={() => setExpanded(false)}>
          Cancel
        </Button>
      </div>
    </div>
  )
}
