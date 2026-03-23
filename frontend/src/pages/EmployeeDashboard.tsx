import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import apiClient from '../api/client'
import type { ReviewSession, SubmitReviewRequest } from '../types'

interface Props {
  employeeId: number
  employeeName: string
  onLogout: () => void
}

function decodeEmployeeIdFromToken(): number | null {
  const token = localStorage.getItem('token')
  if (!token) return null
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    const raw: string | undefined = payload.EmployeeId ?? payload.employeeId
    const id = raw !== undefined ? parseInt(raw, 10) : NaN
    return isNaN(id) ? null : id
  } catch {
    return null
  }
}

export { decodeEmployeeIdFromToken }

export default function EmployeeDashboard({ employeeId, employeeName, onLogout }: Props) {
  const queryClient = useQueryClient()

  const { data: sessions = [], isLoading, isError } = useQuery<ReviewSession[]>({
    queryKey: ['reviewSessions', employeeId],
    queryFn: async () => {
      const { data } = await apiClient.get<ReviewSession[]>(
        `/api/reviewsessions/employee/${employeeId}`
      )
      return data
    },
  })

  const submitMutation = useMutation({
    mutationFn: async ({ id, notes }: { id: number; notes?: string }) => {
      const body: SubmitReviewRequest = { notes }
      await apiClient.post(`/api/reviews/${id}/submit`, body)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reviewSessions', employeeId] })
    },
  })

  const pendingSessions = sessions.filter((s) => s.status === 'Pending')
  const nextScheduled = sessions
    .filter((s) => s.status === 'Pending' && new Date(s.scheduledDate) > new Date())
    .sort(
      (a, b) => new Date(a.scheduledDate).getTime() - new Date(b.scheduledDate).getTime()
    )[0]

  const reviewIsDue =
    pendingSessions.length > 0 &&
    pendingSessions.some((s) => new Date(s.deadline) <= new Date(Date.now() + 30 * 86400_000))

  const dueSession = reviewIsDue
    ? pendingSessions.sort(
        (a, b) => new Date(a.deadline).getTime() - new Date(b.deadline).getTime()
      )[0]
    : null

  return (
    <div className="min-h-screen bg-gray-100">
      {/* Header */}
      <header className="bg-blue-700 text-white px-6 py-4 flex items-center justify-between shadow">
        <h1 className="text-xl font-bold tracking-tight">Performance Review System</h1>
        <div className="flex items-center gap-4">
          <span className="text-blue-200 text-sm">
            Signed in as <span className="font-semibold text-white">{employeeName}</span>
          </span>
          <button
            onClick={onLogout}
            className="bg-blue-900 hover:bg-blue-800 text-white text-sm px-3 py-1.5 rounded transition"
          >
            Log out
          </button>
        </div>
      </header>

      {/* Main content */}
      <main className="max-w-4xl mx-auto px-4 py-8">
        <h2 className="text-2xl font-semibold text-gray-800 mb-6">My Dashboard</h2>

        {/* Performance Review Status Card */}
        <div
          className="bg-white rounded-xl shadow p-6 mb-6"
          style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}
        >
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
            <div className="w-10 h-10 rounded-full bg-blue-100 flex items-center justify-center flex-shrink-0">
              {/* clipboard icon */}
              <svg
                className="w-5 h-5 text-blue-700"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth={2}
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2M9 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M9 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2"
                />
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-gray-800">Performance Review Status</h3>
          </div>

          {isLoading && (
            <p className="text-gray-500 text-sm" role="status">
              Loading review data…
            </p>
          )}

          {isError && (
            <p className="text-red-600 text-sm" role="alert">
              Failed to load review sessions. Please refresh the page.
            </p>
          )}

          {!isLoading && !isError && (
            <div
              style={{
                display: 'flex',
                flexWrap: 'wrap',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: '1rem',
              }}
            >
              {reviewIsDue && dueSession ? (
                <>
                  <div>
                    <p className="text-sm text-gray-600">
                      A performance review is due by{' '}
                      <span className="font-semibold text-red-600">
                        {new Date(dueSession.deadline).toLocaleDateString()}
                      </span>
                      .
                    </p>
                    {submitMutation.isError && (
                      <p className="text-red-600 text-xs mt-1" role="alert">
                        Submission failed. Please try again.
                      </p>
                    )}
                    {submitMutation.isSuccess && (
                      <p className="text-green-600 text-xs mt-1" role="status">
                        Review submitted successfully.
                      </p>
                    )}
                  </div>
                  <button
                    onClick={() => submitMutation.mutate({ id: dueSession.id })}
                    disabled={submitMutation.isPending}
                    className="bg-blue-700 hover:bg-blue-800 disabled:opacity-60 text-white font-semibold px-5 py-2 rounded-lg transition"
                  >
                    {submitMutation.isPending ? 'Submitting…' : 'Start Review'}
                  </button>
                </>
              ) : nextScheduled ? (
                <div>
                  <p className="text-sm text-gray-600">
                    No review currently due. Your next scheduled review is on{' '}
                    <span className="font-semibold text-blue-700">
                      {new Date(nextScheduled.scheduledDate).toLocaleDateString()}
                    </span>
                    .
                  </p>
                </div>
              ) : sessions.length === 0 ? (
                <p className="text-sm text-gray-500">No review sessions scheduled yet.</p>
              ) : (
                <div className="flex items-center gap-2">
                  <span className="inline-block w-2 h-2 rounded-full bg-green-500" />
                  <p className="text-sm text-gray-700">
                    All reviews are up to date. Great work!
                  </p>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Review sessions table */}
        {!isLoading && sessions.length > 0 && (
          <div className="bg-white rounded-xl shadow p-6">
            <h3 className="text-base font-semibold text-gray-700 mb-4">All Review Sessions</h3>
            <div style={{ overflowX: 'auto' }}>
              <table className="w-full text-sm border-collapse">
                <thead>
                  <tr className="bg-blue-50 text-left">
                    <th className="px-3 py-2 font-semibold text-gray-700">ID</th>
                    <th className="px-3 py-2 font-semibold text-gray-700">Status</th>
                    <th className="px-3 py-2 font-semibold text-gray-700">Scheduled Date</th>
                    <th className="px-3 py-2 font-semibold text-gray-700">Deadline</th>
                  </tr>
                </thead>
                <tbody>
                  {sessions.map((s) => (
                    <tr key={s.id} className="border-t border-gray-100">
                      <td className="px-3 py-2 text-gray-600">{s.id}</td>
                      <td className="px-3 py-2">
                        <span
                          className={
                            s.status === 'Completed'
                              ? 'inline-block px-2 py-0.5 rounded-full text-xs font-semibold bg-green-100 text-green-700'
                              : 'inline-block px-2 py-0.5 rounded-full text-xs font-semibold bg-yellow-100 text-yellow-700'
                          }
                        >
                          {s.status}
                        </span>
                      </td>
                      <td className="px-3 py-2 text-gray-600">
                        {new Date(s.scheduledDate).toLocaleDateString()}
                      </td>
                      <td className="px-3 py-2 text-gray-600">
                        {new Date(s.deadline).toLocaleDateString()}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </main>
    </div>
  )
}
