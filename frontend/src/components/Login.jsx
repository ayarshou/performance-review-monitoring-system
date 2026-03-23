import { useState } from 'react'

const DEMO_USERS = [
  { name: 'Alice Johnson',  username: 'alice', role: 'Engineering Manager' },
  { name: 'Bob Martinez',   username: 'bob',   role: 'Product Manager' },
  { name: 'Carol Williams', username: 'carol', role: 'HR Manager' },
  { name: 'David Lee',      username: 'david', role: 'Software Engineer' },
  { name: 'Eva Brown',      username: 'eva',   role: 'Senior Software Engineer' },
  { name: 'Frank Davis',    username: 'frank', role: 'Software Engineer' },
  { name: 'Grace Kim',      username: 'grace', role: 'Junior Software Engineer' },
  { name: 'Henry Wilson',   username: 'henry', role: 'Product Analyst' },
  { name: 'Isla Thompson',  username: 'isla',  role: 'UX Designer' },
  { name: 'Jake Anderson',  username: 'jake',  role: 'Business Analyst' },
  { name: 'Karen White',    username: 'karen', role: 'HR Specialist' },
  { name: 'Liam Garcia',    username: 'liam',  role: 'Recruitment Coordinator' },
  { name: 'Mia Robinson',   username: 'mia',   role: 'HR Coordinator' },
]

export default function Login({ onLogin }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError]       = useState(null)
  const [loading, setLoading]   = useState(false)

  const handleSubmit = async e => {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      })
      if (res.ok) {
        onLogin(await res.json())
      } else {
        const body = await res.json().catch(() => ({}))
        setError(body.message ?? `Login failed (${res.status})`)
      }
    } catch {
      setError('Could not reach the server. Is the API running?')
    } finally {
      setLoading(false)
    }
  }

  const quickLogin = user => {
    setUsername(user.username)
    setPassword('Review2026!')
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <h1>Performance Review System</h1>
        <h2>Sign In</h2>

        <form onSubmit={handleSubmit} className="login-form">
          <label>
            Username
            <input
              value={username}
              onChange={e => setUsername(e.target.value)}
              required
              autoFocus
              autoComplete="username"
              placeholder="e.g. alice"
            />
          </label>
          <label>
            Password
            <input
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              required
              autoComplete="current-password"
              placeholder="Review2026!"
            />
          </label>
          {error && <p className="login-error">{error}</p>}
          <button type="submit" className="login-btn" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign In'}
          </button>
        </form>

        <div className="login-hint">
          <p>
            <strong>Demo accounts</strong> — password for all:{' '}
            <code>Review2026!</code>
          </p>
          <table>
            <thead>
              <tr>
                <th>Name</th>
                <th>Username</th>
                <th>Role</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {DEMO_USERS.map(u => (
                <tr key={u.username}>
                  <td>{u.name}</td>
                  <td><code>{u.username}</code></td>
                  <td>{u.role}</td>
                  <td>
                    <button
                      type="button"
                      className="quick-login-btn"
                      onClick={() => quickLogin(u)}
                    >
                      Use
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
