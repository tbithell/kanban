import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  DialogTrigger,
  Input,
  Label,
  MessageBar,
  MessageBarBody,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useCreateBoard } from '../../hooks/useCreateBoard'

const useStyles = makeStyles({
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
  alert: {
    marginBottom: tokens.spacingVerticalS,
  },
})

export default function CreateBoardDialog() {
  const styles = useStyles()
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [name, setName] = useState('')
  const { mutate, isPending, isError, error, reset } = useCreateBoard()

  const handleSubmit = () => {
    const trimmed = name.trim()
    if (!trimmed) return
    mutate(
      { name: trimmed },
      {
        onSuccess: (board) => {
          setName('')
          setOpen(false)
          navigate(`/boards/${board.id}`)
        },
      }
    )
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(_, d) => {
        setOpen(d.open)
        if (!d.open) {
          setName('')
          reset()
        }
      }}
    >
      <DialogTrigger disableButtonEnhancement>
        <Button appearance="primary" aria-label="Create board">
          Create Board
        </Button>
      </DialogTrigger>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Create Board</DialogTitle>
          <DialogContent>
            {isError && (
              <MessageBar role="alert" intent="error" className={styles.alert}>
                <MessageBarBody>{error?.title ?? 'Failed to create board'}</MessageBarBody>
              </MessageBar>
            )}
            <div className={styles.field}>
              <Label htmlFor="board-name">Board name</Label>
              <Input
                id="board-name"
                aria-label="Board name"
                value={name}
                onChange={(_, d) => setName(d.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleSubmit()
                }}
              />
            </div>
          </DialogContent>
          <DialogActions>
            <Button
              appearance="primary"
              disabled={!name.trim() || isPending}
              onClick={handleSubmit}
            >
              Create
            </Button>
            <DialogTrigger disableButtonEnhancement>
              <Button appearance="secondary">Cancel</Button>
            </DialogTrigger>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  )
}
