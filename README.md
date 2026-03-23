# Performance Review Monitoring System

A full-stack application for managing employee performance reviews, built with **.NET 8 Web API**, **React 18 + TypeScript (Vite)**, and **SQL Server**, fully containerised with Docker.

---

## Quick Start (Docker)

> **Requires:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) 24+ (includes Docker Compose v2).

```bash
# 1. Clone the repository
git clone https://github.com/ayarshou/performance-review-monitoring-system.git
cd performance-review-monitoring-system

# 2. Build and start all three services (database, API, frontend)
docker compose up --build
```

Once all containers are healthy, open your browser:

| Service | URL |
|---------|-----|
| **Frontend** (React app) | <http://localhost:3000> |
| **API** (Swagger UI) | <http://localhost:5000/swagger> |

> The API waits for the database to be ready before starting.  
> EF Core migrations run automatically on first boot — no manual setup needed.

To run the stack **in the background** (detached mode):

```bash
docker compose up --build -d
```

To stop and remove all containers:

```bash
docker compose down
```

---

## Project Structure

```
.
├── api/                        # .NET 8 Web API
│   ├── Controllers/            # REST endpoints (thin; delegate to repositories)
│   ├── Data/                   # AppDbContext (EF Core)
│   ├── Migrations/             # EF Core migrations
│   ├── Models/                 # POCO classes & enums
│   ├── Repositories/           # IEmployeeRepository, IReviewSessionRepository + implementations
│   ├── Services/               # IEmailService (MailKit) + ReviewSchedulerService (BackgroundService)
│   ├── appsettings.json
│   ├── Dockerfile
│   └── PerformanceReviewApi.csproj
├── frontend/                   # React 18 + TypeScript + Vite SPA
│   ├── src/
│   │   ├── __tests__/          # Vitest tests (LoginPage.test.tsx)
│   │   ├── api/                # axios client (client.ts)
│   │   ├── components/         # EmployeeList.tsx, ReviewSessionList.tsx
│   │   ├── pages/              # LoginPage.tsx, EmployeeDashboard.tsx
│   │   ├── types/              # Shared TypeScript types (index.ts)
│   │   ├── App.tsx             # Root component – auth routing
│   │   ├── main.tsx            # ReactDOM entry point
│   │   ├── setupTests.ts       # Vitest global setup
│   │   └── index.css           # Global styles (Tailwind + custom)
│   ├── .env                    # VITE_API_URL (copy from .env.example)
│   ├── .env.example            # Environment variable template
│   ├── tailwind.config.js
│   ├── postcss.config.js
│   ├── tsconfig.json
│   ├── vite.config.ts
│   ├── Dockerfile
│   ├── nginx.conf
│   └── package.json
├── tests/
│   └── PerformanceReviewApi.Tests/   # xUnit + Moq unit & integration tests
├── docker-compose.yml
└── README.md
```

---

## Prerequisites

| Tool | Version |
|------|---------|
| Docker | 24+ |
| Docker Compose | v2 (included with Docker Desktop) |

---

## Build & Run (Docker – recommended)

### 1. Clone the repository
```bash
git clone https://github.com/ayarshou/performance-review-monitoring-system.git
cd performance-review-monitoring-system
```

### 2. Start the entire stack
```bash
# Foreground (logs stream to the terminal)
docker compose up --build

# Detached / background mode
docker compose up --build -d
```

This command will:
- Pull the **SQL Server 2022** image and initialise the database
- Build and start the **.NET 8 API** (auto-runs EF Core migrations on first boot)
- Build and compile the **React + TypeScript frontend** and serve it via Nginx

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

To also remove the database volume:
```bash
docker compose down -v
```

### 5. Rebuild a single service
```bash
# Rebuild only the frontend image
docker compose build frontend

# Rebuild only the API image
docker compose build api
```

### 6. View logs
```bash
# All services
docker compose logs -f

# Single service
docker compose logs -f api
docker compose logs -f frontend
```

---

## Run Locally (without Docker)

### API
```bash
cd api
dotnet restore
# Point to a local SQL Server instance in appsettings.Development.json
dotnet ef database update      # apply migrations
dotnet run
# API is available at http://localhost:5000
```

### Frontend
```bash
cd frontend
cp .env.example .env           # configure VITE_API_URL if needed
npm install
npm run dev                    # starts Vite dev server on http://localhost:5173
```

> **Note:** `vite.config.ts` proxies `/api` requests to `http://localhost:5000` in dev mode.

---

## Frontend Development

### Available scripts

```bash
# Start Vite dev server (hot reload)
npm run dev

# Production build
npm run build

# Preview the production build locally
npm run preview

# Run Vitest tests (single run)
npm test

# Run Vitest in watch mode
npm run test:watch

# Type-check without building
npm run type-check
```

### Environment variables

Copy `.env.example` to `.env` and set the values:

```bash
cp frontend/.env.example frontend/.env
```

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_URL` | *(empty)* | Full API base URL. Leave empty to use Vite/Nginx proxy. Set to `http://localhost:5000` for direct access without a proxy. |

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

## Running Tests

### Backend (xUnit + Moq)
```bash
cd tests/PerformanceReviewApi.Tests
dotnet test
```

### Frontend (Vitest + Testing Library)
```bash
cd frontend
npm test
```

---

## API Endpoints

### Authentication

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/login` | Authenticate and receive a JWT token |

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

### Reviews  *(requires JWT)*

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/reviews/{id}/submit` | Complete a review session (add notes) |
| GET | `/api/reviews/team-status` | Manager: view all subordinate review statuses |

---

## Database Schema

```
Employee
────────────────────────────────────────
Id           INT IDENTITY PK
Name         NVARCHAR(100) NOT NULL
Email        NVARCHAR(200) NOT NULL
Position     NVARCHAR(100) NOT NULL
HireDate     DATETIME2     NOT NULL
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
Username       NVARCHAR(100) NOT NULL  [unique]
PasswordHash   NVARCHAR(MAX) NOT NULL  (PBKDF2: salt:hash)
Role           NVARCHAR(50)  NOT NULL  (Manager | Employee)
EmployeeId     INT           NULL  →  FK → Employee(Id)  [SetNull]
```

**Relationships**

- `Employee.ManagerId → Employee.Id`: self-referencing one-to-many — one Manager has many Subordinates.
- `ReviewSession.EmployeeId → Employee.Id`: one Employee has many ReviewSessions.
- `User.EmployeeId → Employee.Id`: optional link between a login account and an employee record.

---

## Environment Variables

### API (set via `docker-compose.yml` or shell environment)

| Variable | Example | Description |
|----------|---------|-------------|
| `ConnectionStrings__DefaultConnection` | `Server=db;Database=...` | SQL Server connection string |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ASP.NET Core environment |
| `JwtSettings__SecretKey` | *see appsettings.json* | JWT signing secret (replace in production) |

### Frontend (set in `frontend/.env`)

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_URL` | *(empty)* | API base URL; empty = use proxy |
