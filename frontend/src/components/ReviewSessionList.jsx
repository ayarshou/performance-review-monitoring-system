import { useState, useEffect } from 'react'

const API = '/api/reviewsessions'

const emptyForm = {
  employeeId: '',
  status: 'Pending',
  scheduledDate: '',
  deadline: '',
}

export default function ReviewSessionList() {
  const [sessions, setSessions] = useState([])
  const [form, setForm] = useState(emptyForm)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const load = async () => {
    try {
      setLoading(true)
      const res = await fetch(API)
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      setSessions(await res.json())
      setError(null)
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const handleChange = e =>
    setForm(f => ({ ...f, [e.target.name]: e.target.value }))

  const handleSubmit = async e => {
    e.preventDefault()
    const body = {
      employeeId: Number(form.employeeId),
      status: form.status,
      scheduledDate: form.scheduledDate,
      deadline: form.deadline,
    }
    const res = await fetch(API, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    if (res.ok) { setForm(emptyForm); load() }
    else setError(`Failed to create session: ${res.status}`)
  }

  const remove = async id => {
    if (!window.confirm('Delete this review session?')) return
    const res = await fetch(`${API}/${id}`, { method: 'DELETE' })
    if (res.ok) load()
    else setError(`Failed to delete: ${res.status}`)
  }

  const badgeClass = status =>
    status === 'Completed' ? 'badge badge-completed' : 'badge badge-pending'

  return (
    <>
      <section>
        <h2>Schedule Review Session</h2>
        <form onSubmit={handleSubmit}>
          <label>
            Employee ID *
            <input type="number" name="employeeId" value={form.employeeId} onChange={handleChange} required min="1" />
          </label>
          <label>
            Status
            <select name="status" value={form.status} onChange={handleChange}>
              <option value="Pending">Pending</option>
              <option value="Completed">Completed</option>
            </select>
          </label>
          <label>
            Scheduled Date *
            <input type="date" name="scheduledDate" value={form.scheduledDate} onChange={handleChange} required />
          </label>
          <label>
            Deadline *
            <input type="date" name="deadline" value={form.deadline} onChange={handleChange} required />
          </label>
          <div className="full-width">
            <button type="submit" className="btn btn-primary">Schedule Session</button>
          </div>
        </form>
        {error && <p className="error-msg" style={{ marginTop: '0.5rem' }}>{error}</p>}
      </section>

      <section>
        <h2>Review Sessions</h2>
        {loading && <p className="loading-msg">Loading…</p>}
        {!loading && sessions.length === 0 && <p>No review sessions found.</p>}
        {!loading && sessions.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Employee</th>
                <th>Status</th>
                <th>Scheduled Date</th>
                <th>Deadline</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {sessions.map(s => (
                <tr key={s.id}>
                  <td>{s.id}</td>
                  <td>{s.employee?.name ?? s.employeeId}</td>
                  <td><span className={badgeClass(s.status)}>{s.status}</span></td>
                  <td>{s.scheduledDate?.slice(0, 10)}</td>
                  <td>{s.deadline?.slice(0, 10)}</td>
                  <td>
                    <button className="btn btn-danger" onClick={() => remove(s.id)}>Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </>
  )
}
