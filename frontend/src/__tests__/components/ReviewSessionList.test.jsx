import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import ReviewSessionList from '../../components/ReviewSessionList'
import apiClient from '../../api/client'

const mock = new MockAdapter(apiClient)

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
  mock.reset()
  vi.restoreAllMocks()
})

describe('ReviewSessionList', () => {
  it('shows loading state while fetching', () => {
    mock.onGet('/api/reviewsessions').reply(() => new Promise(() => {}))
    render(<ReviewSessionList />)
    expect(screen.getByText(/loading/i)).toBeInTheDocument()
  })

  it('displays sessions from the API', async () => {
    mock.onGet('/api/reviewsessions').reply(200, mockSessions)
    render(<ReviewSessionList />)

    await waitFor(() => expect(screen.getByText('David Lee')).toBeInTheDocument())
    expect(screen.getByText('Eva Brown')).toBeInTheDocument()
  })

  it('renders Pending and Completed status badges', async () => {
    mock.onGet('/api/reviewsessions').reply(200, mockSessions)
    render(<ReviewSessionList />)

    // Use selector:'span' to avoid matching the <select> options in the form
    await waitFor(() => expect(screen.getByText('Pending', { selector: 'span' })).toBeInTheDocument())
    expect(screen.getByText('Completed', { selector: 'span' })).toBeInTheDocument()
  })

  it('applies correct badge CSS classes for statuses', async () => {
    mock.onGet('/api/reviewsessions').reply(200, mockSessions)
    render(<ReviewSessionList />)

    await waitFor(() => {
      // Use selector:'span' to target the badge spans, not the <select> options
      const pendingBadge   = screen.getByText('Pending', { selector: 'span' })
      const completedBadge = screen.getByText('Completed', { selector: 'span' })
      expect(pendingBadge).toHaveClass('badge-pending')
      expect(completedBadge).toHaveClass('badge-completed')
    })
  })

  it('shows empty state when no sessions returned', async () => {
    mock.onGet('/api/reviewsessions').reply(200, [])
    render(<ReviewSessionList />)
    await waitFor(() => expect(screen.getByText(/no review sessions found/i)).toBeInTheDocument())
  })

  it('shows error message when API returns non-OK status', async () => {
    mock.onGet('/api/reviewsessions').reply(500)
    render(<ReviewSessionList />)
    await waitFor(() => expect(screen.getByText(/HTTP 500/i)).toBeInTheDocument())
  })

  it('calls DELETE endpoint when Delete button is clicked and confirmed', async () => {
    mock.onGet('/api/reviewsessions').replyOnce(200, mockSessions)
    mock.onDelete('/api/reviewsessions/1').replyOnce(200)
    mock.onGet('/api/reviewsessions').replyOnce(200, [mockSessions[1]])

    vi.stubGlobal('confirm', () => true)

    const user = userEvent.setup()
    render(<ReviewSessionList />)

    await waitFor(() => expect(screen.getAllByRole('button', { name: /delete/i }).length).toBeGreaterThan(0))
    await user.click(screen.getAllByRole('button', { name: /delete/i })[0])

    await waitFor(() => {
      expect(mock.history.delete.some((request) => request.url?.includes('/api/reviewsessions/1'))).toBe(true)
    })
  })
})
