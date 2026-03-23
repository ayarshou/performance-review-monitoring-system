import { useState, useEffect } from 'react'

const API = '/api/employees'

const emptyForm = {
  name: '',
  email: '',
  position: '',
  hireDate: '',
  managerId: '',
}

export default function EmployeeList() {
  const [employees, setEmployees] = useState([])
  const [form, setForm] = useState(emptyForm)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const load = async () => {
    try {
      setLoading(true)
      const res = await fetch(API)
      if (!res.ok) throw new Error(`HTTP ${res.status}`)
      setEmployees(await res.json())
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
      ...form,
      hireDate: form.hireDate || new Date().toISOString(),
      managerId: form.managerId ? Number(form.managerId) : null,
    }
    const res = await fetch(API, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })
    if (res.ok) { setForm(emptyForm); load() }
    else setError(`Failed to create employee: ${res.status}`)
  }

  const remove = async id => {
    if (!window.confirm('Delete this employee?')) return
    const res = await fetch(`${API}/${id}`, { method: 'DELETE' })
    if (res.ok) load()
    else setError(`Failed to delete: ${res.status}`)
  }

  return (
    <>
      <section>
        <h2>Add Employee</h2>
        <form onSubmit={handleSubmit}>
          <label>
            Name *
            <input name="name" value={form.name} onChange={handleChange} required />
          </label>
          <label>
            Email *
            <input type="email" name="email" value={form.email} onChange={handleChange} required />
          </label>
          <label>
            Position *
            <input name="position" value={form.position} onChange={handleChange} required />
          </label>
          <label>
            Hire Date *
            <input type="date" name="hireDate" value={form.hireDate} onChange={handleChange} required />
          </label>
          <label>
            Manager ID (optional)
            <input type="number" name="managerId" value={form.managerId} onChange={handleChange} min="1" />
          </label>
          <div className="full-width">
            <button type="submit" className="btn btn-primary">Add Employee</button>
          </div>
        </form>
        {error && <p className="error-msg" style={{ marginTop: '0.5rem' }}>{error}</p>}
      </section>

      <section>
        <h2>Employee List</h2>
        {loading && <p className="loading-msg">Loading…</p>}
        {!loading && employees.length === 0 && <p>No employees found.</p>}
        {!loading && employees.length > 0 && (
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Email</th>
                <th>Position</th>
                <th>Hire Date</th>
                <th>Manager ID</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {employees.map(emp => (
                <tr key={emp.id}>
                  <td>{emp.id}</td>
                  <td>{emp.name}</td>
                  <td>{emp.email}</td>
                  <td>{emp.position}</td>
                  <td>{emp.hireDate?.slice(0, 10)}</td>
                  <td>{emp.managerId ?? '—'}</td>
                  <td>
                    <button className="btn btn-danger" onClick={() => remove(emp.id)}>Delete</button>
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
