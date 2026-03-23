import axios from 'axios'

// Pull API base URL from the .env file (VITE_API_URL).
// When empty the browser uses the Vite dev-server proxy (dev) or
// the Nginx reverse proxy (Docker/production) to reach /api/*.
const API_BASE: string = import.meta.env.VITE_API_URL ?? ''

const apiClient = axios.create({
  baseURL: API_BASE,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Attach JWT Bearer token to every request when one is stored in localStorage.
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export default apiClient
