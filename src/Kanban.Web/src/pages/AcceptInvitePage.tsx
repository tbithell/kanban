import {
  FluentProvider,
  webLightTheme,
  Button,
  MessageBar,
  MessageBarBody,
  Title1,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useCurrentUser } from '../hooks/useCurrentUser'
import { type AcceptInviteError, useAcceptInvite } from '../hooks/useAcceptInvite'

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: '100vh',
    gap: tokens.spacingVerticalL,
    textAlign: 'center',
    padding: tokens.spacingHorizontalXXL,
  },
})

export default function AcceptInvitePage() {
  const styles = useStyles()
  const { token } = useParams<{ token: string }>()
  const navigate = useNavigate()
  const { isUnauthenticated } = useCurrentUser()
  const { mutate, data, error, isPending } = useAcceptInvite()

  useEffect(() => {
    if (!isUnauthenticated && token && !data && !error && !isPending) {
      mutate(token)
    }
  }, [isUnauthenticated, token, data, error, isPending, mutate])

  useEffect(() => {
    if (data) {
      navigate('/')
    }
  }, [data, navigate])

  const acceptError = error as AcceptInviteError | null

  return (
    <FluentProvider theme={webLightTheme}>
      <div className={styles.root}>
        <Title1 as="h1">Accept Invitation</Title1>

        {isUnauthenticated && (
          <Button
            as="a"
            appearance="primary"
            href={`/api/v1/auth/signin?returnUrl=${encodeURIComponent(`/accept/${token ?? ''}`)}`}
            aria-label="Accept and Sign in with Google"
          >
            Accept &amp; Sign in with Google
          </Button>
        )}

        {acceptError?.status === 410 && (
          <MessageBar intent="error">
            <MessageBarBody>
              This invitation is no longer valid. Please request a new one.
            </MessageBarBody>
          </MessageBar>
        )}

        {acceptError?.status === 422 && (
          <MessageBar intent="error">
            <MessageBarBody>
              This invitation was issued to a different email address.
            </MessageBarBody>
          </MessageBar>
        )}
      </div>
    </FluentProvider>
  )
}
