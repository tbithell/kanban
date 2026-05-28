import {
  FluentProvider,
  webLightTheme,
  Title1,
  Body1,
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

export default function AcceptInvitePage() {
  const styles = useStyles()

  return (
    <FluentProvider theme={webLightTheme}>
      <div className={styles.root}>
        <Title1 as="h1">Accept Invitation</Title1>
        <Body1>Processing your invitation…</Body1>
      </div>
    </FluentProvider>
  )
}
