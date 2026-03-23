import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import App from '../App'

const fakeUser = {
  id: 1, name: 'Alice Johnson', email: 'alice.johnson@company.com', position: 'Engineering Manager',
}

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('App', () => {
  it('shows Login page when not authenticated', () => {
    render(<App />)
    expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument()
  })

  it('shows app header and navigation after successful login', async () => {
    global.fetch = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(fakeUser) }) // login
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([]) })        // MyReviews

    const user = userEvent.setup()
    render(<App />)

    await user.type(screen.getByPlaceholderText(/e\.g\. alice/i), 'alice')
    await user.type(screen.getByPlaceholderText(/Review2026!/i), 'Review2026!')
    await user.click(screen.getByRole('button', { name: /^sign in$/i }))

    // Wait for the Sign Out button — unique to the authenticated view (not on login page)
    await waitFor(() => expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument())
    // Name appears in both the header <strong> and the MyReviews profile table;
    // use selector to target the header element specifically
    expect(screen.getByText('Alice Johnson', { selector: 'strong' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /my reviews/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /employees/i })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /all review sessions/i })).toBeInTheDocument()
  })

  it('clicking Sign Out returns to the Login page', async () => {
    global.fetch = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(fakeUser) })
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve([]) })

    const user = userEvent.setup()
    render(<App />)

    await user.type(screen.getByPlaceholderText(/e\.g\. alice/i), 'alice')
    await user.type(screen.getByPlaceholderText(/Review2026!/i), 'Review2026!')
    await user.click(screen.getByRole('button', { name: /^sign in$/i }))
    await waitFor(() => expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument())

    await user.click(screen.getByRole('button', { name: /sign out/i }))

    expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument()
  })

  it('tab navigation switches between views', async () => {
    global.fetch = vi.fn()
      .mockResolvedValueOnce({ ok: true, json: () => Promise.resolve(fakeUser) })
      .mockResolvedValue({ ok: true, json: () => Promise.resolve([]) })

    const user = userEvent.setup()
    render(<App />)

    await user.type(screen.getByPlaceholderText(/e\.g\. alice/i), 'alice')
    await user.type(screen.getByPlaceholderText(/Review2026!/i), 'Review2026!')
    await user.click(screen.getByRole('button', { name: /^sign in$/i }))
    // Wait for the Sign Out button — unique to the authenticated view
    await waitFor(() => expect(screen.getByRole('button', { name: /sign out/i })).toBeInTheDocument())

    await user.click(screen.getByRole('button', { name: /employees/i }))
    await waitFor(() => expect(screen.getByText(/employee list/i)).toBeInTheDocument())
  })
})
