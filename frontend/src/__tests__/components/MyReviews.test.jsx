import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import MyReviews from '../../components/MyReviews'

const mockUser = {
  id: 5, name: 'Eva Brown', email: 'eva.brown@company.com', position: 'Senior Software Engineer',
}

const mockSessions = [
  { id: 1, status: 'Pending',   scheduledDate: '2026-03-30T00:00:00Z', deadline: '2026-04-22T00:00:00Z' },
  { id: 2, status: 'Completed', scheduledDate: '2025-09-23T00:00:00Z', deadline: '2025-10-23T00:00:00Z' },
]

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('MyReviews', () => {
  it('shows the logged-in user profile information', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve([]) })
    render(<MyReviews user={mockUser} />)
    expect(screen.getByText('Eva Brown')).toBeInTheDocument()
    expect(screen.getByText('eva.brown@company.com')).toBeInTheDocument()
    expect(screen.getByText('Senior Software Engineer')).toBeInTheDocument()
  })

  it('fetches /api/reviewsessions/employee/{userId} on mount', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve([]) })
    render(<MyReviews user={mockUser} />)
    await waitFor(() => expect(global.fetch).toHaveBeenCalledWith('/api/reviewsessions/employee/5'))
  })

  it('renders sessions in a table after loading', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve(mockSessions) })
    render(<MyReviews user={mockUser} />)

    await waitFor(() => expect(screen.getByText('Pending')).toBeInTheDocument())
    expect(screen.getByText('Completed')).toBeInTheDocument()
    // Two session rows should appear
    expect(screen.getAllByRole('row').length).toBeGreaterThanOrEqual(3) // header + 2 data rows
  })

  it('shows empty state message when no sessions exist', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: true, json: () => Promise.resolve([]) })
    render(<MyReviews user={mockUser} />)

    await waitFor(() =>
      expect(screen.getByText(/no review sessions scheduled/i)).toBeInTheDocument()
    )
  })

  it('shows error message when fetch fails', async () => {
    global.fetch = vi.fn().mockRejectedValue(new Error('Network error'))
    render(<MyReviews user={mockUser} />)

    await waitFor(() => expect(screen.getByText('Network error')).toBeInTheDocument())
  })

  it('shows error message when API returns non-OK status', async () => {
    global.fetch = vi.fn().mockResolvedValue({ ok: false, status: 500 })
    render(<MyReviews user={mockUser} />)

    await waitFor(() => expect(screen.getByText(/HTTP 500/i)).toBeInTheDocument())
  })
})
