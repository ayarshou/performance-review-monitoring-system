import { useState } from 'react'
import EmployeeList from './components/EmployeeList.jsx'
import ReviewSessionList from './components/ReviewSessionList.jsx'

export default function App() {
  const [activeTab, setActiveTab] = useState('employees')

  return (
    <>
      <header>
        <h1>Performance Review System</h1>
        <nav>
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
        </nav>
      </header>

      <main>
        {activeTab === 'employees' ? <EmployeeList /> : <ReviewSessionList />}
      </main>
    </>
  )
}
