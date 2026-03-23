import { useState } from 'react'
import Login from './components/Login.jsx'
import EmployeeList from './components/EmployeeList.jsx'
import ReviewSessionList from './components/ReviewSessionList.jsx'
import MyReviews from './components/MyReviews.jsx'

export default function App() {
  const [currentUser, setCurrentUser] = useState(null)
  const [activeTab, setActiveTab]     = useState('my-reviews')

  if (!currentUser) {
    return <Login onLogin={user => { setCurrentUser(user); setActiveTab('my-reviews') }} />
  }

  return (
    <>
      <header>
        <div className="header-top">
          <h1>Performance Review System</h1>
          <div className="user-bar">
            <span><strong>{currentUser.name}</strong> &mdash; {currentUser.position}</span>
            <button onClick={() => setCurrentUser(null)}>Sign Out</button>
          </div>
        </div>
        <nav>
          <button
            className={activeTab === 'my-reviews' ? 'active' : ''}
            onClick={() => setActiveTab('my-reviews')}
          >
            My Reviews
          </button>
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
            All Review Sessions
          </button>
        </nav>
      </header>

      <main>
        {activeTab === 'my-reviews' && <MyReviews user={currentUser} />}
        {activeTab === 'employees'  && <EmployeeList />}
        {activeTab === 'reviews'    && <ReviewSessionList />}
      </main>
    </>
  )
}
