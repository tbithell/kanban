import {
  FluentProvider,
  webLightTheme,
  Title1,
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
    gap: tokens.spacingVerticalXXL,
  },
})

export default function SignInPage() {
  const styles = useStyles()

  return (
    <FluentProvider theme={webLightTheme}>
      <div className={styles.root}>
        <Title1 as="h1">Sign in to Kanban</Title1>
        <Button as="a" href="/api/v1/auth/signin" appearance="primary" size="large">
          Sign in with Google
        </Button>
      </div>
    </FluentProvider>
  )
}
