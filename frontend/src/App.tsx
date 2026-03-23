import { useState } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import LoginPage from './pages/LoginPage'
import EmployeeDashboard, { decodeEmployeeIdFromToken } from './pages/EmployeeDashboard'
import EmployeeList from './components/EmployeeList'
import ReviewSessionList from './components/ReviewSessionList'

const queryClient = new QueryClient()

function getStoredToken(): string | null {
  return localStorage.getItem('token')
}

function getUsernameFromToken(token: string): string {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    return (payload.unique_name as string | undefined) ?? 'User'
  } catch {
    return 'User'
  }
}

type Tab = 'dashboard' | 'employees' | 'reviews'

export default function App() {
  const [token, setToken] = useState<string | null>(getStoredToken)
  const [activeTab, setActiveTab] = useState<Tab>('dashboard')

  const handleLoginSuccess = () => {
    setToken(localStorage.getItem('token'))
  }

  const handleLogout = () => {
    localStorage.removeItem('token')
    setToken(null)
    queryClient.clear()
  }

  // Not logged in – show login page
  if (!token) {
    return (
      <QueryClientProvider client={queryClient}>
        <LoginPage onLoginSuccess={handleLoginSuccess} />
      </QueryClientProvider>
    )
  }

  const employeeId = decodeEmployeeIdFromToken()
  const username = getUsernameFromToken(token)

  return (
    <QueryClientProvider client={queryClient}>
      {/* If the user is linked to an employee, show the dashboard tab */}
      {employeeId !== null && activeTab === 'dashboard' ? (
        <>
          <EmployeeDashboard
            employeeId={employeeId}
            employeeName={username}
            onLogout={handleLogout}
          />
          <div style={{ maxWidth: 960, margin: '0 auto', padding: '0 1rem 1rem' }}>
            <button className="btn btn-primary" onClick={() => setActiveTab('employees')}>
              Manage Employees
            </button>{' '}
            <button className="btn btn-primary" onClick={() => setActiveTab('reviews')}>
              Manage Review Sessions
            </button>
          </div>
        </>
      ) : (
        <>
          <header>
            <h1>Performance Review System</h1>
            <nav>
              {employeeId !== null && (
                <button
                  className={activeTab === 'dashboard' ? 'active' : ''}
                  onClick={() => setActiveTab('dashboard')}
                >
                  My Dashboard
                </button>
              )}
              <button
                className={activeTab === 'employees' ? 'active' : ''}
                onClick={() => setActiveTab('employees')}
              >
                Employees
              </button>
              <button
                className={activeTab === 'reviews' ? 'active' : ''}
                onClick={() => setActiveTab('reviews')}
              >
                Review Sessions
              </button>
              <button onClick={handleLogout} style={{ marginLeft: 'auto' }}>
                Log out
              </button>
            </nav>
          </header>
          <main>
            {activeTab === 'employees' && <EmployeeList />}
            {activeTab === 'reviews' && <ReviewSessionList />}
          </main>
        </>
      )}
    </QueryClientProvider>
  )
}
