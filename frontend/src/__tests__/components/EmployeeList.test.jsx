import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import EmployeeList from '../../components/EmployeeList'

const mockEmployees = [
  { id: 1, name: 'Alice Johnson', email: 'alice.johnson@company.com', position: 'Engineering Manager', hireDate: '2020-01-01T00:00:00Z', managerId: null, subordinates: [], reviewSessions: [] },
  { id: 4, name: 'David Lee',     email: 'david.lee@company.com',     position: 'Software Engineer',   hireDate: '2023-01-01T00:00:00Z', managerId: 1,    subordinates: [], reviewSessions: [] },
]

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('EmployeeList', () => {
  it('shows loading state while fetching', () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => new Promise(() => {}) })
    render(<EmployeeList />)
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('displays employees from the API', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve(mockEmployees) })
    render(<EmployeeList />)

    await waitFor(() => expect(screen.getByText('Alice Johnson')).toBeInTheDocument())
    expect(screen.getByText('David Lee')).toBeInTheDocument()
    expect(screen.getByText('david.lee@company.com')).toBeInTheDocument()
  })

  it('shows employee management heading and add-employee form', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve([]) })
    render(<EmployeeList />)
    expect(screen.getByRole('heading', { name: /add employee/i })).toBeInTheDocument()
  })

  it('shows empty state when no employees returned', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve([]) })
    render(<EmployeeList />)
    await waitFor(() => expect(screen.getByText(/no employees found/i)).toBeInTheDocument())
  })

  it('shows error message when API returns non-OK status', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: false, status: 503 })
    render(<EmployeeList />)
    await waitFor(() => expect(screen.getByText(/HTTP 503/i)).toBeInTheDocument())
  })

  it('calls DELETE endpoint when Delete button is clicked and confirmed', async () => {
    global.fetch = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockEmployees) }) // initial load
      .mockResolvedValueOnce({ ok: true })                                              // DELETE
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([mockEmployees[1]]) }) // reload

    vi.stubGlobal('confirm', () => true)

    const user = userEvent.setup()
    render(<EmployeeList />)

    await waitFor(() => expect(screen.getAllByRole('button', { name: /delete/i }).length).toBeGreaterThan(0))
    await user.click(screen.getAllByRole('button', { name: /delete/i })[0])

    await waitFor(() => {
      const calls = global.fetch.mock.calls
      expect(calls.some(([url, opts]) => opts?.method === 'DELETE' && url.includes('/api/employees/'))).toBe(true)
    })
  })
})
