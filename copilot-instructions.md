# Copilot Instructions – Performance Review Monitoring System

## Project Overview

This is a full-stack **Performance Review Monitoring System** composed of:

| Layer | Technology |
|-------|-----------|
| Backend API | .NET 8 Web API, C#, Entity Framework Core 8, SQL Server |
| Frontend | React 18 + TypeScript, Vite 6, Tailwind CSS 3, React Query (TanStack Query v5), axios |
| Infrastructure | Docker, Docker Compose, Nginx |

The system allows HR managers to track employee performance review cycles, assign review sessions, and monitor completion status. Employees log in via a JWT-secured Login page and see their personal **EmployeeDashboard**, which displays the current performance review status (due or upcoming). Managers can also view team status via the API. A background scheduler automatically emails reminders to employees and summary reports to managers.

---

## Key Functionality

### Authentication Flow
1. User navigates to the app → `LoginPage` is shown (stored JWT absent or expired).
2. `LoginPage` calls `POST /api/auth/login` via **axios** (`src/api/client.ts`).
3. On success the JWT is stored in `localStorage` under the key `token`.
4. `App.tsx` detects the token and renders the main application; `apiClient` attaches `Authorization: Bearer <token>` to all subsequent requests via an axios request interceptor.
5. Logout clears `localStorage` and the React Query cache.

### EmployeeDashboard
- Mounted when the decoded JWT contains an `EmployeeId` claim.
- Uses **React Query** (`useQuery`) to fetch `GET /api/reviewsessions/employee/{employeeId}`.
- Shows a **Performance Review Status card**:
  - If any `Pending` session has a deadline within the next 30 days → displays **"Start Review"** button.
  - Clicking "Start Review" calls `POST /api/reviews/{id}/submit` via a `useMutation`.
  - If no review is currently due but upcoming sessions exist → shows the **next scheduled date**.
  - If all sessions are completed → shows a "Great work!" completion message.
- Uses **CSS Flexbox** (Tailwind utility classes) for a responsive layout.

### LoginPage
- Clean, modern CSS file (`LoginPage.css`) with centered card using Flexbox, blue/grey corporate colour palette.
- Form fields: `username`, `password`.
- Displays an inline error alert on `401 Unauthorized` (or network failure).
- Button is disabled while the request is in-flight.

### Admin/Manager Views
- **EmployeeList**: CRUD table for managing employees (create, list, delete).
- **ReviewSessionList**: CRUD table for scheduling and managing review sessions.
- Both require a logged-in session (token is sent via the axios interceptor).

---

## Repository Layout

```
api/
  Controllers/
    AuthController.cs            – POST /api/auth/login (issues JWT)
    EmployeesController.cs       – CRUD for Employee (delegates to IEmployeeRepository)
    ReviewController.cs          – Auth-protected: submit review + manager team-status
    ReviewSessionsController.cs  – CRUD for ReviewSession (delegates to IReviewSessionRepository)
  Data/
    AppDbContext.cs               – EF Core DbContext, Fluent API config
  DTOs/
    LoginRequest.cs              – Username + Password for login
    SubmitReviewRequest.cs       – Optional Notes when completing a review
    SubordinateReviewStatusDto.cs – Manager team-status response shape
  Helpers/
    PasswordHashHelper.cs        – PBKDF2 password hashing/verification (Users table)
    PasswordHelper.cs            – PBKDF2 password hashing/verification (Employees table)
  Middleware/
    GlobalExceptionMiddleware.cs – Catches unhandled exceptions, logs, returns 500 JSON
  Migrations/                    – Auto-generated EF Core migrations
  Models/
    Employee.cs                  – Employee POCO
    ReviewSession.cs             – ReviewSession POCO (includes optional Notes field)
    ReviewStatus.cs              – Enum: Pending | Completed
    User.cs                      – User POCO (Username, PasswordHash, Role, EmployeeId FK)
  Repositories/
    IEmployeeRepository.cs / EmployeeRepository.cs
    IReviewSessionRepository.cs / ReviewSessionRepository.cs
    IUserRepository.cs / UserRepository.cs
  Services/
    IEmailService.cs / EmailService.cs   – MailKit SMTP
    ReviewSchedulerService.cs           – BackgroundService: reminders + manager summaries
  Validators/
    SubmitReviewRequestValidator.cs – FluentValidation: Notes max 2000 chars
  Program.cs                     – App bootstrap, DI, JWT, FluentValidation, middleware, auto-migrate
  appsettings.json               – Connection string + JwtSettings + EmailSettings
  appsettings.Development.json   – Local overrides
  PerformanceReviewApi.csproj
  Dockerfile

frontend/
  src/
    __tests__/
      LoginPage.test.tsx          – Vitest + Testing Library tests for LoginPage
    api/
      client.ts                   – axios instance; reads VITE_API_URL from .env; attaches JWT
    components/
      EmployeeList.tsx            – Employee CRUD table
      ReviewSessionList.tsx       – Review session CRUD table
      MyReviews.jsx              – Employee review sessions view
    pages/
      LoginPage.tsx               – Login form (axios, localStorage, error handling)
      LoginPage.css               – Login card styles (blue/grey palette, centered)
      EmployeeDashboard.tsx       – Employee review status card + React Query data fetching
    types/
      index.ts                    – Shared TypeScript interfaces (Employee, ReviewSession, etc.)
    App.tsx                       – Root component; auth-based routing (Login ↔ Dashboard)
    main.tsx                      – ReactDOM entry point; wraps app in QueryClientProvider
    setupTests.ts                 – Vitest global setup (@testing-library/jest-dom)
    vite-env.d.ts                 – VITE_API_URL ImportMeta type declaration
    index.css                     – Tailwind directives + global styles
  .env                            – VITE_API_URL (gitignored, copy from .env.example)
  .env.example                    – Environment variable template
  tailwind.config.cjs             – Tailwind content paths (CommonJS for Docker build)
  postcss.config.cjs              – Tailwind + Autoprefixer PostCSS plugins (CommonJS)
  tsconfig.json                   – TypeScript compiler config (strict, JSX react-jsx)
  tsconfig.node.json              – TypeScript config for vite.config.ts
  vite.config.ts                  – Vite config: React plugin, dev proxy, Vitest settings
  nginx.conf                      – Nginx: serve SPA + proxy /api → backend
  package.json                    – scripts: dev / build / preview / test / type-check
  Dockerfile
  Dockerfile.test                 – Test runner image (Vitest)

api.Tests/                      # Backend unit + integration tests (xUnit + Moq)
  Controllers/                    – Controller unit tests
  Data/                          – DB seeder tests
  Helpers/                       – Password helper tests + test DB factory
  Integration/                   – Full HTTP pipeline tests via WebApplicationFactory

tests/
  PerformanceReviewApi.Tests/
    Integration/
      ReviewIntegrationTests.cs  – WebApplicationFactory integration tests
    Services/
      ReviewSchedulerServiceTests.cs  – xUnit + Moq unit tests for ReviewSchedulerService

docker-compose.yml               – db + api + frontend services
docker-compose.test.yml          – Test runner (api-tests + frontend-tests)
README.md
copilot-instructions.md          – This file
```

---

## Architecture & Layers

```
Browser
    │
    ▼
React SPA (Vite / Nginx)
  LoginPage  ──axios──►  POST /api/auth/login  ──► JWT stored in localStorage
  App.tsx    (reads token, decodes claims)
  EmployeeDashboard  ──React Query──►  GET /api/reviewsessions/employee/{id}
  EmployeeList       ──axios──►  GET|POST|DELETE /api/employees
  ReviewSessionList  ──axios──►  GET|POST|DELETE /api/reviewsessions
    │
    │  HTTP (proxied by Vite dev-server OR Nginx in production)
    ▼
GlobalExceptionMiddleware  (api/Middleware/)
    │
JWT Authentication Middleware
    │
Controller  (api/Controllers/)
    │
Repository Interface  (api/Repositories/I*Repository.cs)
    │
Repository Impl → AppDbContext → SQL Server

BackgroundService (ReviewSchedulerService)
    │  resolves scoped repo via IServiceScopeFactory
    ▼
IReviewSessionRepository  →  IEmailService (MailKit SMTP)
```

---

## Authentication & Authorisation

JWT Bearer tokens issued by `POST /api/auth/login`.  
Token claims: `sub` (userId), `unique_name` (username), `role` ("Manager" or "Employee"), `EmployeeId`.

**Frontend token usage (`src/api/client.ts`):**
```ts
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})
```

**Protecting API endpoints:**
- `[Authorize]` – any valid JWT.
- `[Authorize(Roles = "Manager")]` – Manager role only.

**JWT configuration (`appsettings.json` → `JwtSettings`):**
```json
"JwtSettings": {
  "SecretKey": "PerformanceReviewSystem-SuperSecret-Key-2026!",
  "Issuer": "PerformanceReviewApi",
  "Audience": "PerformanceReviewClient",
  "ExpiresInHours": "8"
}
```

> Replace `SecretKey` with a strong secret (≥ 32 chars) in production.

---

## Domain Model

### Employee
- **Id**, **Name**, **Email**, **Position**, **HireDate** – core fields
- **ManagerId** `int?` – nullable self-referencing FK
- **ReviewSessions** `ICollection<ReviewSession>` – navigation

### ReviewSession
- **Id**, **EmployeeId**, **Status** (`Pending|Completed`), **ScheduledDate**, **Deadline**, **Notes** `string?`

### User
- **Id**, **Username** (unique), **PasswordHash** (PBKDF2 `salt:hash`), **Role**, **EmployeeId** `int?`

### TypeScript types (`frontend/src/types/index.ts`)
- `Employee`, `ReviewSession`, `ReviewStatus`, `LoginRequest`, `LoginResponse`, `DecodedToken`, `SubmitReviewRequest`

---

## React / Frontend Conventions

- **Language**: TypeScript (strict mode). All source files use `.tsx` / `.ts`.
- **Styling**: Tailwind CSS utility classes for layout; custom CSS files for component-specific styles (e.g. `LoginPage.css`).
- **Data fetching**: **React Query** (`@tanstack/react-query`) for server state – queries, mutations, cache invalidation.
- **HTTP client**: **axios** via the shared `src/api/client.ts` instance.  
  The base URL is read from `import.meta.env.VITE_API_URL` (`.env` file).
- **Auth state**: stored in `localStorage` (key: `token`); `App.tsx` reads it on mount.
- **Components**: functional components with hooks only (no class components).
- **API proxy**: `/api` is proxied to `http://localhost:5000` by `vite.config.ts` in dev, and by `nginx.conf` in production Docker.

---

## Testing

### Frontend (Vitest + Testing Library)
- Test files live in `src/__tests__/`.
- Setup file: `src/setupTests.ts` (imports `@testing-library/jest-dom`).
- HTTP mocking: `axios-mock-adapter` wraps the shared `apiClient` for components using axios; plain `global.fetch` mocks for components using fetch directly (e.g. `MyReviews.jsx`).
- Run: `cd frontend && npm test` (executes `vitest run` — single pass, CI-friendly).

### Backend (xUnit + Moq)
- Integration tests use `WebApplicationFactory` with an in-memory DB.
- Unit tests use `Moq` for repositories and `TimeProvider` for deterministic scheduling.
- Run: `cd tests/PerformanceReviewApi.Tests && dotnet test`

### Docker test compose
```bash
docker compose -f docker-compose.test.yml up --build --abort-on-container-exit
```
Runs both frontend and backend test suites in containers. Exit code `0` = all tests passed. No SQL Server or Nginx needed — backend uses an in-memory DB.

---

## Key Conventions

### C# / .NET
- **Namespace**: `PerformanceReviewApi.*` per folder.
- **File-scoped namespaces**, **implicit usings**, **nullable reference types** enabled.
- **Enum serialisation**: `ReviewStatus` stored/serialised as string.
- **Circular reference handling**: `ReferenceHandler.IgnoreCycles` applied globally.
- **Auto-migration**: runs at startup.
- **FluentValidation**: validators in `api/Validators/`, auto-scanned.
- **Password hashing**: Two helpers, both PBKDF2-SHA256 — `PasswordHashHelper` (100 000 iterations, for Users table) and `PasswordHelper` (10 000 iterations, for Employees table).

### Docker
- Multi-stage builds for both API (`sdk→aspnet`) and frontend (`node→nginx`).
- Healthcheck on SQL Server; API waits for DB to be healthy before starting.
- SQL Server password `YourStrong@Passw0rd` – **replace in production**.

---

## Common Tasks (for Copilot)

### Add a new entity
1. POCO in `api/Models/` → `DbSet` + Fluent config in `AppDbContext`.
2. `dotnet ef migrations add <Name>`.
3. `I<Entity>Repository` + implementation in `api/Repositories/`.
4. Register in `Program.cs` as `AddScoped`.
5. Controller in `api/Controllers/`.
6. TypeScript interface in `frontend/src/types/index.ts`.
7. React component in `frontend/src/components/`.

### Protect a new endpoint with JWT
```csharp
[Authorize]                        // any valid JWT
[Authorize(Roles = "Manager")]    // Manager role only
```
Read claims: `User.FindFirst("EmployeeId")?.Value`

### Add FluentValidation to a new endpoint
1. Create validator in `api/Validators/` extending `AbstractValidator<TRequest>`.
2. Inject `IValidator<TRequest>` into the controller.
3. Call `await _validator.ValidateAsync(request)`; return `BadRequest(errors)` when invalid.

### Add a new React Query hook
```ts
const { data, isLoading, isError } = useQuery({
  queryKey: ['myEntity', id],
  queryFn: async () => {
    const { data } = await apiClient.get<MyEntity[]>(`/api/myentity/${id}`)
    return data
  },
})
```

### Create a new user (seeding)
```csharp
await userRepository.CreateAsync(new User {
    Username = "alice",
    PasswordHash = PasswordHashHelper.Hash("SecurePass!"),
    Role = "Manager",
    EmployeeId = 1
});
```

### Change ReviewStatus values
1. Edit `api/Models/ReviewStatus.cs`.
2. Create a new EF migration.
3. Update the `<select>` in `ReviewSessionList.tsx`.
4. Update the `ReviewStatus` union type in `frontend/src/types/index.ts`.

### Add a new API field
1. Update POCO → migration → repository (if new queries needed) → controller validation.
2. Update TypeScript interface in `src/types/index.ts`.
3. Update form + table in the corresponding React component.

### Run the full stack
```bash
docker compose up --build
```

### Run all tests in Docker
```bash
docker compose -f docker-compose.test.yml up --build --abort-on-container-exit
```

### Run frontend tests (locally)
```bash
cd frontend && npm test
```

### Run backend tests (locally)
```bash
cd tests/PerformanceReviewApi.Tests && dotnet test
```

### Apply DB migrations manually
```bash
cd api && dotnet ef database update
```
