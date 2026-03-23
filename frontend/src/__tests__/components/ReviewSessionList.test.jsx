import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import ReviewSessionList from '../../components/ReviewSessionList'

const mockSessions = [
  {
    id: 1, employeeId: 4,
    employee: { id: 4, name: 'David Lee' },
    status: 'Pending',
    scheduledDate: '2026-03-30T00:00:00Z',
    deadline: '2026-04-22T00:00:00Z',
  },
  {
    id: 11, employeeId: 5,
    employee: { id: 5, name: 'Eva Brown' },
    status: 'Completed',
    scheduledDate: '2025-09-23T00:00:00Z',
    deadline: '2025-10-23T00:00:00Z',
  },
]

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('ReviewSessionList', () => {
  it('shows loading state while fetching', () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => new Promise(() => {}) })
    render(<ReviewSessionList />)
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('displays sessions from the API', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve(mockSessions) })
    render(<ReviewSessionList />)

    await waitFor(() => expect(screen.getByText('David Lee')).toBeInTheDocument())
    expect(screen.getByText('Eva Brown')).toBeInTheDocument()
  })

  it('renders Pending and Completed status badges', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve(mockSessions) })
    render(<ReviewSessionList />)

    await waitFor(() => expect(screen.getByText('Pending')).toBeInTheDocument())
    expect(screen.getByText('Completed')).toBeInTheDocument()
  })

  it('applies correct badge CSS classes for statuses', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve(mockSessions) })
    render(<ReviewSessionList />)

    await waitFor(() => {
      const pendingBadge   = screen.getByText('Pending')
      const completedBadge = screen.getByText('Completed')
      expect(pendingBadge).toHaveClass('badge-pending')
      expect(completedBadge).toHaveClass('badge-completed')
    })
  })

  it('shows empty state when no sessions returned', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve([]) })
    render(<ReviewSessionList />)
    await waitFor(() => expect(screen.getByText(/no review sessions found/i)).toBeInTheDocument())
  })

  it('shows error message when API returns non-OK status', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: false, status: 500 })
    render(<ReviewSessionList />)
    await waitFor(() => expect(screen.getByText(/HTTP 500/i)).toBeInTheDocument())
  })

  it('calls DELETE endpoint when Delete button is clicked and confirmed', async () => {
    global.fetch = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(mockSessions) })
      .mockResolvedValueOnce({ ok: true })
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([mockSessions[1]]) })

    vi.stubGlobal('confirm', () => true)

    const user = userEvent.setup()
    render(<ReviewSessionList />)

    await waitFor(() => expect(screen.getAllByRole('button', { name: /delete/i }).length).toBeGreaterThan(0))
    await user.click(screen.getAllByRole('button', { name: /delete/i })[0])

    await waitFor(() => {
      const calls = global.fetch.mock.calls
      expect(calls.some(([url, opts]) => opts?.method === 'DELETE' && url.includes('/api/reviewsessions/'))).toBe(true)
    })
  })
})
