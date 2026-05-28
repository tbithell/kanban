import { Navigate, Route, Routes } from 'react-router-dom'
import type { ReactNode } from 'react'
import { useCurrentUser } from './hooks/useCurrentUser'
import SignInPage from './pages/SignInPage'
import NotRegisteredPage from './pages/NotRegisteredPage'
import AcceptInvitePage from './pages/AcceptInvitePage'

function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isLoading, isUnauthenticated, isNotRegistered } = useCurrentUser()

  if (isLoading) return null
  if (isUnauthenticated) return <Navigate to="/signin" replace />
  if (isNotRegistered) return <Navigate to="/not-registered" replace />

  return <>{children}</>
}

export default function App() {
  return (
    <Routes>
      <Route path="/signin" element={<SignInPage />} />
      <Route path="/not-registered" element={<NotRegisteredPage />} />
      <Route path="/accept/:token" element={<AcceptInvitePage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <main aria-label="Kanban board">
              <p>Welcome to Kanban</p>
            </main>
          </ProtectedRoute>
        }
      />
    </Routes>
  )
}
