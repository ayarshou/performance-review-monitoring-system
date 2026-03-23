import { useState, useEffect } from 'react'

export default function MyReviews({ user }) {
  const [sessions, setSessions] = useState([])
  const [loading, setLoading]   = useState(true)
  const [error, setError]       = useState(null)

  useEffect(() => {
    fetch(`/api/reviewsessions/employee/${user.id}`)
      .then(r => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`)
        return r.json()
      })
      .then(data => { setSessions(data); setLoading(false) })
      .catch(e  => { setError(e.message); setLoading(false) })
  }, [user.id])

  const badgeClass = s =>
    s === 'Completed' ? 'badge badge-completed' : 'badge badge-pending'

  return (
    <>
      <section>
        <h2>My Profile</h2>
        <table>
          <tbody>
            <tr><th>Name</th>    <td>{user.name}</td></tr>
            <tr><th>Email</th>   <td>{user.email}</td></tr>
            <tr><th>Position</th><td>{user.position}</td></tr>
          </tbody>
        </table>
      </section>

      <section>
        <h2>My Review Sessions</h2>
        {loading && <p className="loading-msg">Loading…</p>}
        {error   && <p className="error-msg">{error}</p>}
        {!loading && !error && sessions.length === 0 && (
          <p>No review sessions scheduled yet.</p>
        )}
        {sessions.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Status</th>
                <th>Scheduled Date</th>
                <th>Deadline</th>
              </tr>
            </thead>
            <tbody>
              {sessions.map(s => (
                <tr key={s.id}>
                  <td>{s.id}</td>
                  <td><span className={badgeClass(s.status)}>{s.status}</span></td>
                  <td>{new Date(s.scheduledDate).toLocaleDateString()}</td>
                  <td>{new Date(s.deadline).toLocaleDateString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </>
  )
}
