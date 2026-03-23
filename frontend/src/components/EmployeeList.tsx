import { useState, useEffect } from 'react'
import type { Employee } from '../types'
import apiClient from '../api/client'

const API = '/api/employees'

const emptyForm = {
  name: '',
  email: '',
  position: '',
  hireDate: '',
  managerId: '',
}

export default function EmployeeList() {
  const [employees, setEmployees] = useState<Employee[]>([])
  const [form, setForm] = useState(emptyForm)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const load = async () => {
    try {
      setLoading(true)
      const { data } = await apiClient.get<Employee[]>(API)
      setEmployees(data)
      setError(null)
    } catch {
      setError('Failed to load employees.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm((f) => ({ ...f, [e.target.name]: e.target.value }))

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const body = {
      ...form,
      hireDate: form.hireDate || new Date().toISOString(),
      managerId: form.managerId ? Number(form.managerId) : null,
    }
    try {
      await apiClient.post(API, body)
      setForm(emptyForm)
      void load()
    } catch {
      setError('Failed to create employee.')
    }
  }

  const remove = async (id: number) => {
    if (!window.confirm('Delete this employee?')) return
    try {
      await apiClient.delete(`${API}/${id}`)
      void load()
    } catch {
      setError('Failed to delete employee.')
    }
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
            <input
              type="date"
              name="hireDate"
              value={form.hireDate}
              onChange={handleChange}
              required
            />
          </label>
          <label>
            Manager ID (optional)
            <input
              type="number"
              name="managerId"
              value={form.managerId}
              onChange={handleChange}
              min="1"
            />
          </label>
          <div className="full-width">
            <button type="submit" className="btn btn-primary">
              Add Employee
            </button>
          </div>
        </form>
        {error && (
          <p className="error-msg" style={{ marginTop: '0.5rem' }}>
            {error}
          </p>
        )}
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
              {employees.map((emp) => (
                <tr key={emp.id}>
                  <td>{emp.id}</td>
                  <td>{emp.name}</td>
                  <td>{emp.email}</td>
                  <td>{emp.position}</td>
                  <td>{emp.hireDate?.slice(0, 10)}</td>
                  <td>{emp.managerId ?? '—'}</td>
                  <td>
                    <button className="btn btn-danger" onClick={() => remove(emp.id)}>
                      Delete
                    </button>
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
