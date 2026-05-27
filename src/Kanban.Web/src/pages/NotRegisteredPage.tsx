import {
  FluentProvider,
  webLightTheme,
  Title1,
  Body1,
  Button,
  makeStyles,
  tokens,
} from '@fluentui/react-components'

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

export default function NotRegisteredPage() {
  const styles = useStyles()

  const handleSignOut = () => {
    fetch('/api/v1/auth/signout', { method: 'POST' }).finally(() => {
      window.location.href = '/signin'
    })
  }

  return (
    <FluentProvider theme={webLightTheme}>
      <div className={styles.root}>
        <Title1 as="h1">Access Denied — Not Registered</Title1>
        <Body1>
          Your account is not registered. Access is by invitation only. Please contact the
          administrator to request an invite.
        </Body1>
        <Button appearance="secondary" onClick={handleSignOut}>
          Sign out
        </Button>
      </div>
    </FluentProvider>
  )
}
