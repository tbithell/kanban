import {
  FluentProvider,
  webLightTheme,
  Button,
  Input,
  Label,
  MessageBar,
  MessageBarBody,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { useEffect, useState } from 'react'
import { type IssueInviteError, useIssueInvite } from '../../hooks/useIssueInvite'

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
    padding: tokens.spacingHorizontalL,
  },
  field: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalXS,
  },
})

interface InviteUserDialogProps {
  open: boolean
  onDismiss: () => void
}

export default function InviteUserDialog({ open, onDismiss }: InviteUserDialogProps) {
  const styles = useStyles()
  const [email, setEmail] = useState('')
  const { mutate, data, error, isPending, reset } = useIssueInvite()

  useEffect(() => {
    if (!open) reset()
  }, [open, reset])

  if (!open) return null

  const inviteError = error as IssueInviteError | null

  const handleSubmit = () => {
    if (email.trim()) mutate(email.trim())
  }

  const handleDismiss = () => {
    setEmail('')
    onDismiss()
  }

  return (
    <FluentProvider theme={webLightTheme}>
      <div className={styles.root} role="dialog" aria-modal="true" aria-label="Invite user">
        <div className={styles.field}>
          <Label htmlFor="invite-email">Email address</Label>
          <Input
            id="invite-email"
            type="email"
            aria-label="Email address"
            value={email}
            onChange={(_, d) => setEmail(d.value)}
          />
        </div>

        {inviteError?.status === 409 && (
          <MessageBar intent="error">
            <MessageBarBody>
              This email address is already registered. Please use a different address.
            </MessageBarBody>
          </MessageBar>
        )}

        {inviteError?.status === 422 && (
          <MessageBar intent="error">
            <MessageBarBody>Must be a valid email address.</MessageBarBody>
          </MessageBar>
        )}

        {data && (
          <div className={styles.field}>
            <Label htmlFor="redemption-link">Redemption link</Label>
            <Input
              id="redemption-link"
              aria-label="Redemption link"
              readOnly
              value={data.redemptionLink}
            />
          </div>
        )}

        <Button appearance="primary" disabled={!email.trim() || isPending} onClick={handleSubmit}>
          Send Invitation
        </Button>

        <Button appearance="secondary" onClick={handleDismiss}>
          Close
        </Button>
      </div>
    </FluentProvider>
  )
}
