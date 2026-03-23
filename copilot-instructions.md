# Copilot Instructions – Performance Review Monitoring System

## Project Overview

This is a full-stack **Performance Review Monitoring System** composed of:

| Layer | Technology |
|-------|-----------|
| Backend API | .NET 8 Web API, C#, Entity Framework Core 8, SQL Server |
| Frontend | React 18 (Vite), plain CSS |
| Infrastructure | Docker, Docker Compose, Nginx |

The system allows HR managers to track employee performance review cycles, assign review sessions, and monitor completion status.

---

## Repository Layout

```
api/
  Controllers/
    EmployeesController.cs       – CRUD for Employee
    ReviewSessionsController.cs  – CRUD for ReviewSession
  Data/
    AppDbContext.cs               – EF Core DbContext, Fluent API config
  Migrations/                    – Auto-generated EF Core migrations
  Models/
    Employee.cs                  – Employee POCO
    ReviewSession.cs             – ReviewSession POCO
    ReviewStatus.cs              – Enum: Pending | Completed
  Program.cs                     – App bootstrap, DI, middleware, auto-migrate
  appsettings.json               – Connection string (Docker target: "db")
  appsettings.Development.json   – Connection string (localhost)
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

docker-compose.yml               – db + api + frontend services
README.md
copilot-instructions.md          – This file
```

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
- **Employee** `Employee` – navigation property  

### Relationships
- **Manager → Subordinates**: one-to-many self-referencing on `Employee`. Delete behaviour: `RESTRICT` (cannot delete a manager while they have subordinates).  
- **Employee → ReviewSessions**: one-to-many. Delete behaviour: `CASCADE`.

---

## Key Conventions

### C# / .NET
- **Namespace**: `PerformanceReviewApi` (root), sub-namespaces per folder (`*.Models`, `*.Data`, `*.Controllers`).
- **File-scoped namespaces** (`namespace Foo;`) for all C# files.
- **Implicit usings** and **nullable reference types** are enabled.
- **Enum serialisation**: `ReviewStatus` is stored as a string in SQL Server and serialised as a string in JSON (`JsonStringEnumConverter`).
- **Circular reference handling**: `ReferenceHandler.IgnoreCycles` is applied globally.
- **Auto-migration**: `db.Database.Migrate()` runs at startup so the schema is always current.
- **CORS**: permissive default policy (any origin/method/header) – tighten for production.
- **Swagger**: always enabled (both dev and production inside Docker).

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
5. Create a controller in `api/Controllers/`.
6. Add a React component in `frontend/src/components/`.

### Change ReviewStatus values
Edit `api/Models/ReviewStatus.cs`. If adding a value, create a new EF migration. Update the `<select>` in `ReviewSessionList.jsx`.

### Add a new API field
1. Update the POCO in `api/Models/`.
2. Create an EF migration (`dotnet ef migrations add ...`).
3. Update the controller if validation is needed.
4. Update the corresponding React form and table.

### Run the stack
```bash
docker compose up --build
```

### Apply DB migrations manually
```bash
cd api && dotnet ef database update
```
