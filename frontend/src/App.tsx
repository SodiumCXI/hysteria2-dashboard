import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import { setRouteSalt } from './utils/routePrefix'

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const token = localStorage.getItem('token')
  return token ? <>{children}</> : <Navigate to="/login" replace />
}

function App() {
  const segments = window.location.pathname.split('/').filter(Boolean)
  const routeSalt = segments.length > 0 ? segments[0] : null
  if (!routeSalt) return null
  setRouteSalt(routeSalt)

  return (
    <BrowserRouter basename={'/' + routeSalt}>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route
          path="/"
          element={
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  )
}

export default App