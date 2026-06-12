import { Text, makeStyles, tokens } from '@fluentui/react-components'
import type { Card } from '../../hooks/useBoard'

const useStyles = makeStyles({
  preview: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalS,
    paddingLeft: tokens.spacingHorizontalM,
    paddingRight: tokens.spacingHorizontalS,
    boxShadow: tokens.shadow16,
    minWidth: '200px',
    maxWidth: '280px',
    opacity: 0.9,
  },
})

interface CardDragPreviewProps {
  card: Card
}

export default function CardDragPreview({ card }: CardDragPreviewProps) {
  const styles = useStyles()
  return (
    <div className={styles.preview}>
      <Text>{card.title}</Text>
    </div>
  )
}
