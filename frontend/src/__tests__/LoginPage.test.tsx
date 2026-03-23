import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import MockAdapter from 'axios-mock-adapter'
import LoginPage from '../pages/LoginPage'
import apiClient from '../api/client'

const mock = new MockAdapter(apiClient)

describe('LoginPage', () => {
  beforeEach(() => {
    mock.reset()
    localStorage.clear()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('renders username and password fields and a sign-in button', () => {
    render(<LoginPage onLoginSuccess={vi.fn()} />)
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
  })

  it('shows an error message when credentials are invalid (401)', async () => {
    mock.onPost('/api/auth/login').reply(401, { message: 'Invalid username or password.' })

    render(<LoginPage onLoginSuccess={vi.fn()} />)

    await userEvent.type(screen.getByLabelText(/username/i), 'wronguser')
    await userEvent.type(screen.getByLabelText(/password/i), 'wrongpassword')
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
      expect(screen.getByRole('alert')).toHaveTextContent('Invalid username or password.')
    })
  })

  it('shows a generic error when the server is unreachable', async () => {
    mock.onPost('/api/auth/login').networkError()

    render(<LoginPage onLoginSuccess={vi.fn()} />)

    await userEvent.type(screen.getByLabelText(/username/i), 'admin')
    await userEvent.type(screen.getByLabelText(/password/i), 'password')
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument()
      expect(screen.getByRole('alert')).toHaveTextContent(
        'Unable to connect to the server. Please try again.'
      )
    })
  })

  it('does not store a token or call onLoginSuccess on failure', async () => {
    mock.onPost('/api/auth/login').reply(401, { message: 'Invalid username or password.' })

    const onLoginSuccess = vi.fn()
    render(<LoginPage onLoginSuccess={onLoginSuccess} />)

    await userEvent.type(screen.getByLabelText(/username/i), 'bad')
    await userEvent.type(screen.getByLabelText(/password/i), 'creds')
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument())

    expect(localStorage.getItem('token')).toBeNull()
    expect(onLoginSuccess).not.toHaveBeenCalled()
  })

  it('stores the token and calls onLoginSuccess on successful login', async () => {
    mock.onPost('/api/auth/login').reply(200, { token: 'fake.jwt.token' })

    const onLoginSuccess = vi.fn()
    render(<LoginPage onLoginSuccess={onLoginSuccess} />)

    await userEvent.type(screen.getByLabelText(/username/i), 'admin')
    await userEvent.type(screen.getByLabelText(/password/i), 'correct')
    await userEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => expect(onLoginSuccess).toHaveBeenCalledOnce())
    expect(localStorage.getItem('token')).toBe('fake.jwt.token')
  })

  it('disables the submit button while the request is in-flight', async () => {
    let resolve!: () => void
    mock.onPost('/api/auth/login').reply(
      () =>
        new Promise<[number, unknown]>((res) => {
          resolve = () => res([200, { token: 'tok' }])
        })
    )

    render(<LoginPage onLoginSuccess={vi.fn()} />)

    await userEvent.type(screen.getByLabelText(/username/i), 'admin')
    await userEvent.type(screen.getByLabelText(/password/i), 'pw')

    const btn = screen.getByRole('button', { name: /sign in/i })
    await userEvent.click(btn)

    // Button should be disabled during the pending request
    expect(btn).toBeDisabled()

    resolve()
    await waitFor(() => expect(btn).not.toBeDisabled())
  })
})
