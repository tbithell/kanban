import { Navigate, Route, Routes } from 'react-router-dom'
import { lazy, Suspense, type ReactNode } from 'react'
import { useCurrentUser } from './hooks/useCurrentUser'
import SignInPage from './pages/SignInPage'
import NotRegisteredPage from './pages/NotRegisteredPage'
import AcceptInvitePage from './pages/AcceptInvitePage'

const BoardListPage = lazy(() => import('./pages/BoardListPage'))
const BoardPage = lazy(() => import('./pages/BoardPage'))

function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isLoading, isUnauthenticated, isNotRegistered, isError } = useCurrentUser()

  if (isLoading) return null
  if (isNotRegistered) return <Navigate to="/not-registered" replace />
  if (isUnauthenticated || isError) return <Navigate to="/signin" replace />

  return <>{children}</>
}

export default function App() {
  return (
    <Suspense>
      <Routes>
        <Route path="/signin" element={<SignInPage />} />
        <Route path="/not-registered" element={<NotRegisteredPage />} />
        <Route path="/accept/:token" element={<AcceptInvitePage />} />
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <BoardListPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/boards/:boardId"
          element={
            <ProtectedRoute>
              <BoardPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </Suspense>
  )
}
