# Performance Review Monitoring System

A full-stack application for managing employee performance reviews, built with **.NET 8 Web API**, **React (Vite)**, and **SQL Server**, fully containerised with Docker.

---

## Project Structure

```
.
├── api/                        # .NET 8 Web API
│   ├── Controllers/            # REST endpoints
│   ├── Data/                   # AppDbContext (EF Core)
│   ├── Migrations/             # EF Core migrations
│   ├── Models/                 # POCO classes & enums
│   ├── appsettings.json
│   ├── Dockerfile
│   └── PerformanceReviewApi.csproj
├── frontend/                   # React + Vite SPA
│   ├── src/
│   │   ├── components/
│   │   ├── App.jsx
│   │   └── main.jsx
│   ├── Dockerfile
│   ├── nginx.conf
│   └── package.json
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
docker compose up --build
```

This command will:
- Pull the **SQL Server 2022** image and initialise the database
- Build and start the **.NET 8 API** (auto-runs EF Core migrations on first boot)
- Build and start the **React frontend** via Nginx

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

---

## Run Locally (without Docker)

### API
```bash
cd api
dotnet restore
# Point to a local SQL Server instance in appsettings.Development.json
dotnet ef database update      # apply migrations
dotnet run
```

### Frontend
```bash
cd frontend
npm install
npm run dev          # starts Vite dev server on http://localhost:5173
```

> **Note:** The Vite dev-server proxies `/api` requests to `http://api:8080`.  
> When running locally, update `vite.config.js` to point `target` at `http://localhost:5000`.

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

## API Endpoints

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
```

**Relationships**

- `Employee.ManagerId → Employee.Id`: self-referencing one-to-many — one Manager has many Subordinates.
- `ReviewSession.EmployeeId → Employee.Id`: one Employee has many ReviewSessions.

---

## Environment Variables

The API reads its connection string from the environment:

```
ConnectionStrings__DefaultConnection=Server=db;Database=PerformanceReviewDb;...
```

This is set automatically by `docker-compose.yml`. Override it in your own environment or a `.env` file for custom deployments.

