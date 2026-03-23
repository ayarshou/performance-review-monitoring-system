# Performance Review Monitoring System

A full-stack application for managing employee performance reviews. Employees and their hierarchical relationships are tracked, and review sessions with scheduled dates and statuses can be created and managed.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend API | .NET 8 Web API + Entity Framework Core |
| Database | SQL Server 2022 |
| Frontend | React 18 + TypeScript + Vite 6 + Tailwind CSS (served by Nginx in production) |
| Containerisation | Docker + Docker Compose |

---

## Project Structure

```
.
├── api/                        # .NET 8 Web API
│   ├── Controllers/
│   │   ├── AuthController.cs           # POST /api/auth/login (issues JWT)
│   │   ├── EmployeesController.cs      # CRUD for employees
│   │   ├── ReviewController.cs         # Submit review + manager team-status
│   │   └── ReviewSessionsController.cs # CRUD for review sessions
│   ├── Data/
│   │   ├── AppDbContext.cs             # EF Core DB context
│   │   └── DbSeeder.cs                # Initial seed data
│   ├── DTOs/                           # Request/Response shapes
│   ├── Helpers/
│   │   ├── PasswordHashHelper.cs       # PBKDF2 hashing (Users table)
│   │   └── PasswordHelper.cs           # PBKDF2 hashing (Employees table)
│   ├── Middleware/
│   │   └── GlobalExceptionMiddleware.cs
│   ├── Migrations/                     # EF Core auto-migrations
│   ├── Models/
│   │   ├── Employee.cs                 # Employee entity
│   │   ├── ReviewSession.cs            # ReviewSession entity
│   │   ├── ReviewStatus.cs             # Pending | Completed enum
│   │   └── User.cs                     # User entity (login credentials)
│   ├── Repositories/                   # Data-access interfaces + implementations
│   ├── Services/
│   │   ├── EmailService.cs             # MailKit SMTP email sending
│   │   └── ReviewSchedulerService.cs   # Background reminder scheduler
│   ├── Validators/
│   │   └── SubmitReviewRequestValidator.cs  # FluentValidation
│   ├── appsettings.json
│   ├── Dockerfile
│   ├── Program.cs                      # DI, JWT auth, middleware, auto-migrate
│   └── PerformanceReviewApi.csproj
├── frontend/                   # React + TypeScript + Vite SPA
│   ├── src/
│   │   ├── api/
│   │   │   └── client.ts               # axios instance with JWT interceptor
│   │   ├── components/
│   │   │   ├── EmployeeList.tsx
│   │   │   ├── ReviewSessionList.tsx
│   │   │   └── MyReviews.jsx
│   │   ├── pages/
│   │   │   ├── LoginPage.tsx
│   │   │   ├── LoginPage.css
│   │   │   └── EmployeeDashboard.tsx
│   │   ├── types/
│   │   │   └── index.ts                # Shared TypeScript interfaces
│   │   ├── __tests__/                  # Vitest + React Testing Library tests
│   │   ├── App.tsx
│   │   ├── main.tsx
│   │   └── index.css                   # Tailwind directives
│   ├── Dockerfile
│   ├── Dockerfile.test                 # Test runner image
│   ├── nginx.conf                      # Nginx: serve SPA + proxy /api
│   ├── package.json
│   ├── tailwind.config.cjs
│   ├── postcss.config.cjs
│   ├── vite.config.ts
│   └── tsconfig.json
├── api.Tests/                  # Backend unit tests (xUnit + Moq)
├── tests/
│   └── PerformanceReviewApi.Tests/     # Integration + scheduler tests
├── docker-compose.yml                  # Full stack (db + api + frontend)
├── docker-compose.test.yml             # Test runner (api-tests + frontend-tests)
├── copilot-instructions.md
└── README.md
```

---

## Data Model

### Employee
| Field | Type | Notes |
|-------|------|-------|
| `Id` | int | Primary key |
| `Name` | string | |
| `Email` | string | |
| `Username` | string | Used for login (expected to be unique) |
| `Position` | string | |
| `HireDate` | DateTime | |
| `ManagerId` | int? | Self-referencing FK (nullable for top-level) |
| `PasswordHash` | string | Stored in DB only; never returned by the API |

### ReviewSession
| Field | Type | Notes |
|-------|------|-------|
| `Id` | int | Primary key |
| `EmployeeId` | int | FK → Employee |
| `Status` | enum | `Pending` \| `Completed` |
| `ScheduledDate` | DateTime | |
| `Deadline` | DateTime | |
| `Notes` | string? | Optional notes when completing a review |

### User
| Field | Type | Notes |
|-------|------|-------|
| `Id` | int | Primary key |
| `Username` | string | Unique login name |
| `PasswordHash` | string | PBKDF2-SHA256 `salt:hash` – never returned by the API |
| `Role` | string | `Manager` or `Employee` |
| `EmployeeId` | int? | FK → Employee (nullable) |

---

## API Endpoints

Base URL (Docker): `http://localhost:5000`  
Swagger UI: `http://localhost:5000/swagger`

### Authentication — `/api/auth`
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/login` | Authenticate and receive a JWT token |

The login endpoint accepts `{ "username": "...", "password": "..." }` and returns a JWT along with user profile data. Credentials are checked against the **Users** table first, then the **Employees** table as a fallback.

### Employees — `/api/employees`
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/employees` | List all employees (with manager & subordinates) |
| GET | `/api/employees/{id}` | Get employee by ID (includes review sessions) |
| POST | `/api/employees` | Create a new employee |
| PUT | `/api/employees/{id}` | Update an employee |
| DELETE | `/api/employees/{id}` | Delete an employee |

### Review Sessions — `/api/reviewsessions`
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/reviewsessions` | List all review sessions |
| GET | `/api/reviewsessions/{id}` | Get session by ID |
| GET | `/api/reviewsessions/employee/{employeeId}` | Get all sessions for an employee |
| POST | `/api/reviewsessions` | Create a new review session |
| PUT | `/api/reviewsessions/{id}` | Update a review session |
| DELETE | `/api/reviewsessions/{id}` | Delete a review session |

---

## Prerequisites

| Tool | Version |
|------|---------|
| Docker | 24+ |
| Docker Compose | v2 (included with Docker Desktop) |

---

## Build & Run with Docker (recommended)

### 1. Clone the repository
```bash
git clone https://github.com/ayarshou/performance-review-monitoring-system.git
cd performance-review-monitoring-system
```

### 2. Start the entire stack
```bash
docker compose up --build
```

This single command will:
1. Pull the **SQL Server 2022** image and start the database
2. Build and start the **.NET 8 API** — EF Core migrations run automatically on first boot
3. Build and start the **React frontend** behind Nginx, with `/api` requests proxied to the backend

Logs from all three services will appear in the same terminal. Wait until you see output similar to:
```
api-1       | Now listening on: http://[::]:8080
frontend-1  | nginx: [notice] start worker processes
```

### 3. Open the application

| Service | URL |
|---------|-----|
| Frontend (React) | <http://localhost:3000> |
| API (Swagger UI) | <http://localhost:5000/swagger> |
| SQL Server | `localhost:1433` (user: `sa`, password: `YourStrong@Passw0rd`) |

### 4. Stop the stack
```bash
docker compose down
```

To also remove the persistent database volume:
```bash
docker compose down -v
```

### Run in detached mode (background)
```bash
docker compose up --build -d
docker compose logs -f          # stream logs afterwards
docker compose down             # stop when done
```

---

## Configuration

Environment variables are set in `docker-compose.yml`. The key ones are:

| Variable | Default | Description |
|----------|---------|-------------|
| `SA_PASSWORD` | `YourStrong@Passw0rd` | SQL Server SA password |
| `ConnectionStrings__DefaultConnection` | *(set in compose)* | Full ADO.NET connection string used by the API |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |

To use a different SA password, update it in both the `db` and `api` service sections of `docker-compose.yml`.

---

## Run Locally (without Docker)

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org)
- A local SQL Server instance

### API
```bash
cd api
dotnet restore
# Update the connection string in appsettings.Development.json to point at your local SQL Server
dotnet ef database update      # apply migrations
dotnet run                     # listens on http://localhost:5000
```

### Frontend
```bash
cd frontend
npm install
npm run dev          # Vite dev server on http://localhost:5173
```

> **Important:** `vite.config.js` proxies `/api` to `http://api:8080`, which only resolves inside Docker.  
> For local development, change the proxy `target` to `http://localhost:5000`:
> ```js
> proxy: {
>   '/api': {
>     target: 'http://localhost:5000',
>     changeOrigin: true,
>   },
> },
> ```

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| API exits immediately with a connection error | SQL Server is still starting up | The `healthcheck` in `docker-compose.yml` retries 10×; wait a few seconds and check `docker compose logs db` |
| `npm run build` fails with "Cannot find module" | Missing `node_modules` | Run `npm install` inside `frontend/` before building |
| Port 3000 or 5000 already in use | Another process is using the port | Change the host-side port in `docker-compose.yml` (e.g. `"3001:80"`) |
| SQL Server container crashes on Apple Silicon / ARM | Image may not support ARM | Use `mcr.microsoft.com/azure-sql-edge` as the `db` image instead |
| Frontend shows blank page after reload | SPA routing | Nginx's `try_files $uri $uri/ /index.html` already handles this; hard-refresh with Ctrl+Shift+R |

---

## Testing

### Run all tests in Docker (no local SDK or Node required)

Both test suites have dedicated Dockerfiles so you can run them with nothing but Docker installed.

#### Run both suites together (recommended)

```bash
docker compose -f docker-compose.test.yml up --build --abort-on-container-exit
```

Exit code `0` means all tests passed. No SQL Server, Nginx, or local tooling needed — the backend uses an in-memory database.

#### Run backend tests only (xUnit + Moq)

```bash
docker build -f tests/PerformanceReviewApi.Tests/Dockerfile -t prms-api-tests .
docker run --rm prms-api-tests
```

#### Run frontend tests only (Vitest)

```bash
docker build -f frontend/Dockerfile.test -t prms-frontend-tests frontend
docker run --rm prms-frontend-tests
```

---

### Backend tests (run locally without Docker)

The backend test project is in `tests/PerformanceReviewApi.Tests/` and uses **xUnit + Moq** with the **EF Core InMemory** provider — no database required.

```bash
cd tests/PerformanceReviewApi.Tests
dotnet test
```

With detailed output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

With coverage (requires `coverlet.collector`):
```bash
dotnet test --collect:"XPlat Code Coverage"
```

#### Test categories

| Category | Location | What's covered |
|----------|----------|----------------|
| Unit — `ReviewSchedulerService` | `Services/` | Email dispatch, deadline logic, idempotency |
| Integration | `Integration/` | Full HTTP pipeline via `WebApplicationFactory`, seeded in-memory data |

---

### Frontend tests (run locally without Docker)

The frontend uses **Vitest** + **React Testing Library** + **jsdom** — no browser required.

```bash
cd frontend
npm install          # first time only
npm test             # single run (CI-friendly, matches Docker behaviour)
npm run test:watch   # interactive watch mode
npm run test:coverage
```

#### Test categories

| Test file | What's covered |
|-----------|----------------|
| `App.test.jsx` | Shows Login when unauthenticated; shows nav after login; Sign Out flow; tab switching |
| `Login.test.jsx` | Form render; demo-user table (13 rows); quick-fill; submit body; onLogin callback; error states |
| `MyReviews.test.jsx` | Profile display; correct fetch endpoint; session table; empty/error states |
| `EmployeeList.test.jsx` | Loading state; employee table render; empty state; error state; DELETE call |
| `ReviewSessionList.test.jsx` | Loading state; session table; badge CSS classes; empty/error states; DELETE call |

#### Run frontend tests

```bash
cd frontend
npm install          # first time only
npm test             # single run (CI-friendly, matches Docker behaviour)
npm run test:watch   # interactive watch mode
npm run test:coverage
```

---

## EF Core Migrations

```bash
cd api

# Add a new migration
dotnet ef migrations add <MigrationName>

# Apply migrations to the database
dotnet ef database update

# Revert the last migration
dotnet ef migrations remove
```

---

## API Endpoints (quick reference)

### Authentication  `POST /api/auth/login`

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/login` | Authenticate and receive a JWT |

### Employees  `GET|POST /api/employees`  ·  `GET|PUT|DELETE /api/employees/{id}`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/employees` | List all employees (with manager & subordinates) |
| GET | `/api/employees/{id}` | Get a single employee with reviews |
| POST | `/api/employees` | Create an employee |
| PUT | `/api/employees/{id}` | Update an employee |
| DELETE | `/api/employees/{id}` | Delete an employee |

### Review Sessions  `GET|POST /api/reviewsessions`  ·  `GET|PUT|DELETE /api/reviewsessions/{id}`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/reviewsessions` | List all review sessions |
| GET | `/api/reviewsessions/{id}` | Get a single session |
| GET | `/api/reviewsessions/employee/{employeeId}` | Sessions for one employee |
| POST | `/api/reviewsessions` | Schedule a review session |
| PUT | `/api/reviewsessions/{id}` | Update a session |
| DELETE | `/api/reviewsessions/{id}` | Delete a session |

---

## Database Schema

```
Employee
────────────────────────────────────────
Id           INT IDENTITY PK
Name         NVARCHAR(100) NOT NULL
Email        NVARCHAR(200) NOT NULL
Username     NVARCHAR(50)  NULL      (unique, used for login)
Position     NVARCHAR(100) NOT NULL
HireDate     DATETIME2     NOT NULL
PasswordHash NVARCHAR(MAX) NULL      (PBKDF2-SHA256, never returned by API)
ManagerId    INT           NULL  →  FK → Employee(Id)   [self-ref, RESTRICT]

ReviewSession
────────────────────────────────────────
Id             INT IDENTITY PK
EmployeeId     INT          NOT NULL  →  FK → Employee(Id)  [CASCADE]
Status         NVARCHAR     NOT NULL  (Pending | Completed)
ScheduledDate  DATETIME2    NOT NULL
Deadline       DATETIME2    NOT NULL
Notes          NVARCHAR(MAX) NULL

User
────────────────────────────────────────
Id             INT IDENTITY PK
Username       NVARCHAR(200) NOT NULL  (unique)
PasswordHash   NVARCHAR(MAX) NOT NULL  (PBKDF2-SHA256)
Role           NVARCHAR(50)  NOT NULL  (Manager | Employee)
EmployeeId     INT           NULL  →  FK → Employee(Id)
```

**Relationships**

- `Employee.ManagerId → Employee.Id`: self-referencing one-to-many — one Manager has many Subordinates.
- `ReviewSession.EmployeeId → Employee.Id`: one Employee has many ReviewSessions.
- `User.EmployeeId → Employee.Id`: optional link between login user and employee record.

---

## Environment Variables

The API reads its connection string from the environment:

```
ConnectionStrings__DefaultConnection=Server=db;Database=PerformanceReviewDb;...
```

This is set automatically by `docker-compose.yml`. Override it in your own environment or a `.env` file for custom deployments.

