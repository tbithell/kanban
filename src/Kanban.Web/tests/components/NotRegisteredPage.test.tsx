import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import NotRegisteredPage from '../../src/pages/NotRegisteredPage'

describe('NotRegisteredPage', () => {
  it('renders a level-1 heading — WCAG AA gate', () => {
    render(<NotRegisteredPage />)
    expect(
      screen.getByRole('heading', { level: 1, name: /not registered|access denied/i })
    ).toBeInTheDocument()
  })

  it('displays an explanatory message about invite-only access', () => {
    render(<NotRegisteredPage />)
    expect(screen.getByText(/invitation|invite/i)).toBeInTheDocument()
  })

  it('provides a way to sign out or try a different account', () => {
    render(<NotRegisteredPage />)
    expect(screen.getByRole('button', { name: /sign out|try another/i })).toBeInTheDocument()
  })
})
