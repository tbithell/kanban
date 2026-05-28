import {
  FluentProvider,
  webLightTheme,
  Button,
  Input,
  Label,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { useIssueInvite } from '../../hooks/useIssueInvite'

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

export default function InviteUserDialog({ open }: InviteUserDialogProps) {
  const styles = useStyles()
  const { isPending } = useIssueInvite()

  if (!open) return null

  return (
    <FluentProvider theme={webLightTheme}>
      <div className={styles.root} role="dialog" aria-modal="true" aria-label="Invite user">
        <div className={styles.field}>
          <Label htmlFor="invite-email">Email address</Label>
          <Input id="invite-email" type="email" aria-label="Email address" />
        </div>
        <Button appearance="primary" disabled={isPending}>
          Send Invitation
        </Button>
      </div>
    </FluentProvider>
  )
}
