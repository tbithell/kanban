import { render, screen } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import SignInPage from '../../src/pages/SignInPage'

describe('SignInPage', () => {
  it('renders a level-1 heading — WCAG AA gate', () => {
    render(<SignInPage />)
    expect(screen.getByRole('heading', { level: 1, name: /sign in/i })).toBeInTheDocument()
  })

  it('renders a sign in with Google button', () => {
    render(<SignInPage />)
    expect(screen.getByRole('link', { name: /sign in with google/i })).toBeInTheDocument()
  })

  it('sign-in link points to the auth signin endpoint', () => {
    render(<SignInPage />)
    const link = screen.getByRole('link', { name: /sign in with google/i })
    expect(link).toHaveAttribute('href', '/api/v1/auth/signin')
  })
})
