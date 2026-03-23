export interface Employee {
  id: number
  name: string
  email: string
  position: string
  hireDate: string
  managerId: number | null
  manager?: Employee
  subordinates?: Employee[]
  reviewSessions?: ReviewSession[]
}

export interface ReviewSession {
  id: number
  employeeId: number
  employee?: Employee
  status: ReviewStatus
  scheduledDate: string
  deadline: string
  notes?: string | null
}

export type ReviewStatus = 'Pending' | 'Completed'

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
}

export interface DecodedToken {
  sub: string
  unique_name: string
  role: string
  EmployeeId?: string
  exp: number
}

export interface SubmitReviewRequest {
  notes?: string
}
