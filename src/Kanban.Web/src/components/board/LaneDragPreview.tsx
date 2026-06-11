import { Title2, makeStyles, tokens } from '@fluentui/react-components'
import type { Lane } from '../../hooks/useBoard'

const useStyles = makeStyles({
  preview: {
    minWidth: '260px',
    maxWidth: '320px',
    backgroundColor: tokens.colorNeutralBackground2,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalM,
    boxShadow: tokens.shadow16,
    opacity: 0.9,
  },
})

interface LaneDragPreviewProps {
  lane: Lane
}

export default function LaneDragPreview({ lane }: LaneDragPreviewProps) {
  const styles = useStyles()
  return (
    <div className={styles.preview}>
      <Title2 as="h2">{lane.name}</Title2>
    </div>
  )
}
