# Copilot Instructions – Performance Review Monitoring System

## Project Overview

This is a full-stack **Performance Review Monitoring System** composed of:

| Layer | Technology |
|-------|-----------|
| Backend API | .NET 8 Web API, C#, Entity Framework Core 8, SQL Server |
| Frontend | React 18 (Vite), plain CSS |
| Infrastructure | Docker, Docker Compose, Nginx |

The system allows HR managers to track employee performance review cycles, assign review sessions, and monitor completion status. A background scheduler automatically emails reminders to employees and summary reports to managers. JWT-based authentication controls access to sensitive operations.

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
    AppDbContext.cs               – EF Core DbContext, Fluent API config (Employee, ReviewSession, User)
  DTOs/
    LoginRequest.cs              – Username + Password for login
    SubmitReviewRequest.cs       – Optional Notes when completing a review
    SubordinateReviewStatusDto.cs – Manager team-status response shape
  Helpers/
    PasswordHashHelper.cs        – PBKDF2 password hashing/verification (no external packages)
  Middleware/
    GlobalExceptionMiddleware.cs – Catches unhandled exceptions, logs, returns 500 JSON
  Migrations/                    – Auto-generated EF Core migrations
  Models/
    Employee.cs                  – Employee POCO
    ReviewSession.cs             – ReviewSession POCO (includes optional Notes field)
    ReviewStatus.cs              – Enum: Pending | Completed
    User.cs                      – User POCO (Username, PasswordHash, Role, EmployeeId FK)
  Repositories/
    IEmployeeRepository.cs       – Repository interface for Employee CRUD + GetSubordinatesWithReviewsAsync
    EmployeeRepository.cs        – EF Core implementation of IEmployeeRepository
    IReviewSessionRepository.cs  – Repository interface for ReviewSession CRUD + scheduler queries
    ReviewSessionRepository.cs   – EF Core implementation of IReviewSessionRepository
    IUserRepository.cs           – Repository interface: GetByUsernameAsync, CreateAsync
    UserRepository.cs            – EF Core implementation of IUserRepository
  Services/
    IEmailService.cs             – Email sending interface
    EmailService.cs              – MailKit SMTP implementation (settings from EmailSettings config)
    ReviewSchedulerService.cs    – BackgroundService: daily review reminders and manager summaries
  Validators/
    SubmitReviewRequestValidator.cs – FluentValidation: Notes max 2000 chars
  Program.cs                     – App bootstrap, DI, JWT auth, FluentValidation, middleware, auto-migrate
  appsettings.json               – Connection string + JwtSettings + EmailSettings
  appsettings.Development.json   – Local overrides (connection string + JwtSettings)
  PerformanceReviewApi.csproj
  Dockerfile

frontend/
  src/
    components/
      EmployeeList.jsx           – Employee table + create/delete form
      ReviewSessionList.jsx      – Review session table + create/delete form
    App.jsx                      – Root component with tab navigation
    main.jsx                     – ReactDOM entry point
    index.css                    – Global styles
  index.html
  vite.config.js                 – Vite + proxy /api → backend
  nginx.conf                     – Nginx: serve SPA + proxy /api
  package.json
  Dockerfile

tests/
  PerformanceReviewApi.Tests/
    Integration/
      ReviewIntegrationTests.cs  – WebApplicationFactory integration tests (login → submit → verify DB)
    Services/
      ReviewSchedulerServiceTests.cs  – xUnit + Moq tests for ReviewSchedulerService

docker-compose.yml               – db + api + frontend services
README.md
copilot-instructions.md          – This file
```

---

## Architecture & Layers

```
HTTP Request
    │
    ▼
GlobalExceptionMiddleware  (api/Middleware/)
    │
    ▼
JWT Authentication / Authorization Middleware
    │
    ▼
Controller  (api/Controllers/)
    │  depends on interface
    ▼
Repository Interface  (api/Repositories/I*Repository.cs)
    │  implemented by
    ▼
Repository Impl  (api/Repositories/*Repository.cs)
    │  uses
    ▼
AppDbContext  (api/Data/AppDbContext.cs)
    │
    ▼
SQL Server

BackgroundService (api/Services/ReviewSchedulerService.cs)
    │  resolves scoped repository via IServiceScopeFactory
    ▼
IReviewSessionRepository
    │  also uses
    ▼
IEmailService  →  EmailService (MailKit)
```

**Key DI registrations (`Program.cs`):**
- `AppDbContext` → scoped  
- `IEmployeeRepository` / `IReviewSessionRepository` / `IUserRepository` → scoped  
- `IEmailService` → singleton (stateless)  
- `ReviewSchedulerService` → hosted service (singleton `BackgroundService`)
- FluentValidation validators → scanned from assembly via `AddValidatorsFromAssemblyContaining<Program>()`

---

## Authentication & Authorisation

JWT Bearer tokens are issued by `POST /api/auth/login`.  
Token claims: `sub` (userId), `unique_name` (username), `role` ("Manager" or "Employee"), `EmployeeId`.

**Protecting endpoints:**
- Add `[Authorize]` to require any valid JWT.
- Add `[Authorize(Roles = "Manager")]` to restrict to Managers only.

**Configuration (`appsettings.json` → `JwtSettings`):**

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
- **Id** `int` – primary key  
- **Name** `string` – required, max 100  
- **Email** `string` – required, max 200  
- **Position** `string` – required, max 100  
- **HireDate** `DateTime` – required  
- **ManagerId** `int?` – nullable FK to `Employee.Id` (self-reference)  
- **Manager** `Employee?` – navigation property  
- **Subordinates** `ICollection<Employee>` – navigation property  
- **ReviewSessions** `ICollection<ReviewSession>` – navigation property  

### ReviewSession
- **Id** `int` – primary key  
- **EmployeeId** `int` – FK to `Employee.Id` (cascade delete)  
- **Status** `ReviewStatus` – enum, stored as string (`Pending` / `Completed`)  
- **ScheduledDate** `DateTime` – required  
- **Deadline** `DateTime` – required  
- **Notes** `string?` – optional review notes, max 2000 chars  
- **Employee** `Employee` – navigation property  

### User
- **Id** `int` – primary key  
- **Username** `string` – required, max 100, unique index  
- **PasswordHash** `string` – PBKDF2 hash stored as `salt:hash` (Base64)  
- **Role** `string` – "Manager" or "Employee"  
- **EmployeeId** `int?` – optional FK to `Employee.Id` (SetNull on delete)  
- **Employee** `Employee?` – navigation property  

### Relationships
- **Manager → Subordinates**: one-to-many self-referencing on `Employee`. Delete behaviour: `RESTRICT`.  
- **Employee → ReviewSessions**: one-to-many. Delete behaviour: `CASCADE`.
- **User → Employee**: optional one-to-one (many users could share an Employee in theory). Delete behaviour: `SetNull`.

---

## Repository Pattern

Both Employee and ReviewSession repositories implement the same base structure:

| Method | Description |
|--------|-------------|
| `GetAllAsync()` | Return all records with navigation properties |
| `GetByIdAsync(id)` | Return single record with navigation properties, or `null` |
| `CreateAsync(entity)` | Add, save, return persisted entity |
| `UpdateAsync(entity)` | Update if exists (returns `false` when not found) |
| `DeleteAsync(id)` | Delete if exists (returns `false` when not found) |
| `ExistsAsync(id)` | True/false existence check |

`IEmployeeRepository` also exposes:

| Method | Used by |
|--------|---------|
| `GetSubordinatesWithReviewsAsync(managerId)` | `ReviewController.GetTeamStatus` |

`IReviewSessionRepository` also exposes two scheduler-specific queries:

| Method | Used by |
|--------|---------|
| `GetPendingDueInRangeAsync(from, to)` | Monthly reminder pass |
| `GetPendingNearDeadlineAsync(deadlineCutoff)` | 3-day manager summary pass |

`IUserRepository` exposes:

| Method | Used by |
|--------|---------|
| `GetByUsernameAsync(username)` | `AuthController.Login` |
| `CreateAsync(user)` | Seeding / admin tooling |

---

## ReviewSchedulerService

Runs every **24 hours** as a `BackgroundService`. Each run:

1. **Monthly reminders** (`SendMonthlyReviewNotificationsAsync`):  
   Queries `GetPendingDueInRangeAsync(monthStart, monthEnd)` and emails each employee.

2. **Manager summaries** (`SendManagerSummariesAsync`):  
   Queries `GetPendingNearDeadlineAsync(today + 3 days)`, groups by manager, and sends one consolidated email per manager. Employees without a manager are silently skipped.

Each run creates a fresh DI scope via `IServiceScopeFactory` so the scoped `IReviewSessionRepository` (and its underlying `AppDbContext`) is properly lifetime-managed.

`TimeProvider` is injected (defaults to `TimeProvider.System`) to enable deterministic testing without real-clock dependency.

---

## EmailService

Reads settings from `appsettings.json` under the `EmailSettings` section:

```json
"EmailSettings": {
  "Host": "smtp.example.com",
  "Port": 587,
  "Username": "notifications@example.com",
  "Password": "",
  "FromAddress": "notifications@example.com",
  "FromName": "Performance Review System"
}
```

Throws `InvalidOperationException` at startup if `Host` or `FromAddress` are missing.  
Wraps every send in `try/catch`; logs the error and re-throws on failure.

---

## Key Conventions

### C# / .NET
- **Namespace**: `PerformanceReviewApi` (root), sub-namespaces per folder (`*.Models`, `*.Data`, `*.Controllers`, `*.Repositories`, `*.Services`, `*.DTOs`, `*.Helpers`, `*.Middleware`, `*.Validators`).
- **File-scoped namespaces** (`namespace Foo;`) for all C# files.
- **Implicit usings** and **nullable reference types** are enabled.
- **Enum serialisation**: `ReviewStatus` is stored as a string in SQL Server and serialised as a string in JSON (`JsonStringEnumConverter`).
- **Circular reference handling**: `ReferenceHandler.IgnoreCycles` is applied globally.
- **Auto-migration**: `db.Database.Migrate()` runs at startup (skipped for non-relational providers like InMemory).
- **CORS**: permissive default policy (any origin/method/header) – tighten for production.
- **Swagger**: always enabled (both dev and production inside Docker), with Bearer auth support.
- **FluentValidation**: validators placed in `api/Validators/`, auto-scanned via `AddValidatorsFromAssemblyContaining<Program>()`, injected into controllers as `IValidator<T>`.
- **Password hashing**: uses built-in `Rfc2898DeriveBytes.Pbkdf2` (SHA-256, 100 000 iterations) via `PasswordHashHelper`. No external crypto packages required.

### React / Vite
- Components are functional with hooks (`useState`, `useEffect`).
- API calls use the native `fetch` API.
- `/api` requests are reverse-proxied via Nginx (`nginx.conf`) in production and via Vite's `server.proxy` in development.

### Docker
- The **API** Dockerfile uses a multi-stage build (`sdk` → `aspnet`).
- The **frontend** Dockerfile uses a multi-stage build (`node` → `nginx:alpine`).
- `docker-compose.yml` includes a **healthcheck** on SQL Server so the API only starts after the DB is ready.
- The SQL Server password used everywhere is `YourStrong@Passw0rd` – **replace with a secret in production**.

---

## Common Tasks (for Copilot)

### Add a new entity
1. Create a POCO in `api/Models/`.
2. Add a `DbSet<T>` in `AppDbContext`.
3. Add Fluent API configuration in `AppDbContext.OnModelCreating`.
4. Run `dotnet ef migrations add <Name>`.
5. Create `I<Entity>Repository` + `<Entity>Repository` in `api/Repositories/`.
6. Register the repository in `Program.cs` as `AddScoped`.
7. Create a controller in `api/Controllers/` that injects the repository interface.
8. Add a React component in `frontend/src/components/`.

### Protect a new endpoint with JWT
1. Add `[Authorize]` to the controller class or action method.
2. For role-based access: `[Authorize(Roles = "Manager")]`.
3. Read user claims via `User.FindFirst("EmployeeId")?.Value` or `User.FindFirst(ClaimTypes.Role)?.Value`.

### Add FluentValidation to a new endpoint
1. Create a validator in `api/Validators/` that extends `AbstractValidator<TRequest>`.
2. Inject `IValidator<TRequest>` into the controller.
3. Call `await _validator.ValidateAsync(request)` and return `BadRequest(validation.Errors)` when invalid.

### Create a new user (programmatic seeding)
```csharp
var user = new User {
    Username = "alice",
    PasswordHash = PasswordHashHelper.Hash("SecurePass!"),
    Role = "Manager",
    EmployeeId = 1
};
await userRepository.CreateAsync(user);
```

### Change ReviewStatus values
Edit `api/Models/ReviewStatus.cs`. If adding a value, create a new EF migration. Update the `<select>` in `ReviewSessionList.jsx`.

### Add a new API field
1. Update the POCO in `api/Models/`.
2. Create an EF migration (`dotnet ef migrations add ...`).
3. Update the repository if new queries are needed.
4. Update the controller if validation is needed.
5. Update the corresponding React form and table.

### Run the stack
```bash
docker compose up --build
```

### Apply DB migrations manually
```bash
cd api && dotnet ef database update
```

### Run all tests (unit + integration)
```bash
cd tests/PerformanceReviewApi.Tests && dotnet test
```

