import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import Login from '../../components/Login'

const TOTAL_DEMO_USERS = 13

beforeEach(() => {
  vi.restoreAllMocks()
})

describe('Login', () => {
  it('renders the sign-in form with username and password fields', () => {
    render(<Login onLogin={() => {}} />)
    expect(screen.getByRole('heading', { name: /sign in/i })).toBeInTheDocument()
    expect(screen.getByPlaceholderText(/e\.g\. alice/i)).toBeInTheDocument()
    expect(screen.getByPlaceholderText(/Review2026!/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /^sign in$/i })).toBeInTheDocument()
  })

  it(`renders demo accounts table with ${TOTAL_DEMO_USERS} rows and Use buttons`, () => {
    render(<Login onLogin={() => {}} />)
    expect(screen.getAllByRole('button', { name: /^use$/i })).toHaveLength(TOTAL_DEMO_USERS)
    // Spot-check first and last demo user
    expect(screen.getByText('alice')).toBeInTheDocument()
    expect(screen.getByText('mia')).toBeInTheDocument()
  })

  it('quick-fill "Use" button populates username and password fields', async () => {
    const user = userEvent.setup()
    render(<Login onLogin={() => {}} />)

    const [firstUseBtn] = screen.getAllByRole('button', { name: /^use$/i })
    await user.click(firstUseBtn)

    // First demo user is alice
    expect(screen.getByPlaceholderText(/e\.g\. alice/i)).toHaveValue('alice')
    expect(screen.getByPlaceholderText(/Review2026!/i)).toHaveValue('Review2026!')
  })

  it('submits correct JSON body to /api/auth/login', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve({ id: 1, name: 'Alice Johnson', email: 'alice@co.com', position: 'Engineering Manager' }),
    })
    const user = userEvent.setup()
    render(<Login onLogin={() => {}} />)

    await user.type(screen.getByPlaceholderText(/e\.g\. alice/i), 'alice')
    await user.type(screen.getByPlaceholderText(/Review2026!/i), 'Review2026!')
    await user.click(screen.getByRole('button', { name: /^sign in$/i }))

    await waitFor(() => expect(global.fetch).toHaveBeenCalledOnce())
    const [url, options] = global.fetch.mock.calls[0]
    expect(url).toBe('/api/auth/login')
    expect(options.method).toBe('POST')
    const body = JSON.parse(options.body)
    expect(body.username).toBe('alice')
    expect(body.password).toBe('Review2026!')
  })

  it('calls onLogin callback with user data on success', async () => {
    const mockUser = { id: 1, name: 'Alice Johnson', email: 'alice@co.com', position: 'Engineering Manager' }
    global.fetch = vi.fn().mockResolvedValue({
      ok: true, json: () => Promise.resolve(mockUser),
    })
    const onLogin = vi.fn()
    const user = userEvent.setup()
    render(<Login onLogin={onLogin} />)

    await user.type(screen.getByPlaceholderText(/e\.g\. alice/i), 'alice')
    await user.type(screen.getByPlaceholderText(/Review2026!/i), 'Review2026!')
    await user.click(screen.getByRole('button', { name: /^sign in$/i }))

    await waitFor(() => expect(onLogin).toHaveBeenCalledWith(mockUser))
  })

  it('shows error message when login fails with 401', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false, status: 401,
      json: () => Promise.resolve({ message: 'Invalid username or password.' }),
    })
    const user = userEvent.setup()
    render(<Login onLogin={() => {}} />)

    await user.type(screen.getByPlaceholderText(/e\.g\. alice/i), 'alice')
    await user.type(screen.getByPlaceholderText(/Review2026!/i), 'wrongpass')
    await user.click(screen.getByRole('button', { name: /^sign in$/i }))

    await waitFor(() =>
      expect(screen.getByText(/invalid username or password/i)).toBeInTheDocument()
    )
  })

  it('shows network error message when fetch throws', async () => {
    global.fetch = vi.fn().mockRejectedValue(new Error('Failed to fetch'))
    const user = userEvent.setup()
    render(<Login onLogin={() => {}} />)

    await user.type(screen.getByPlaceholderText(/e\.g\. alice/i), 'alice')
    await user.type(screen.getByPlaceholderText(/Review2026!/i), 'pass')
    await user.click(screen.getByRole('button', { name: /^sign in$/i }))

    await waitFor(() =>
      expect(screen.getByText(/could not reach the server/i)).toBeInTheDocument()
    )
  })
})
