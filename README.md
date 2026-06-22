# Home Project Management

Full-stack app orchestrated with **.NET Aspire**:

- **Frontend** — Next.js 16 (App Router, TypeScript) — `src/web`
- **Backend** — .NET 10 Web API (minimal APIs + EF Core) — `src/HomeProjectManagement.ApiService`
- **Database** — PostgreSQL (run as a container by Aspire)
- **Orchestration** — .NET Aspire AppHost wires everything together — `src/HomeProjectManagement.AppHost`

## Structure

```
home-project-management/
├── HomeProjectManagement.slnx              # solution
├── src/
│   ├── HomeProjectManagement.AppHost/          # Aspire orchestrator — START HERE
│   ├── HomeProjectManagement.ServiceDefaults/  # shared telemetry / health / resilience
│   ├── HomeProjectManagement.ApiService/       # .NET Web API + EF Core (Npgsql)
│   │   ├── Models/Project.cs
│   │   ├── Data/AppDbContext.cs
│   │   └── Program.cs                          # /api/projects CRUD
│   └── web/                                     # Next.js frontend
│       └── app/
│           ├── page.tsx                        # projects list + create form
│           ├── actions.ts                      # server actions (create/delete)
│           └── lib/api.ts                       # backend client
```

## How it's wired

`AppHost.cs` declares the resources and dependencies:

- `AddPostgres("postgres")` with a persistent data volume + pgAdmin, and a `projectsdb` database.
- `AddProject<...ApiService>("apiservice")` references `projectsdb` and waits for it.
- `AddNextJsApp("web", "../web")` runs `npm run dev`, references the API, and receives the
  backend URL via the `API_BASE_URL` environment variable.

The API reads its connection string from Aspire via `AddNpgsqlDbContext<AppDbContext>("projectsdb")`.
For local dev the schema is created on startup with `EnsureCreated()` — switch to EF Core migrations
for production.

## Prerequisites

- .NET 10 SDK
- Node.js 18+ / npm
- A container runtime for PostgreSQL — **Docker Desktop** or **Rancher Desktop** must be **running**.

## Run

```bash
dotnet run --project src/HomeProjectManagement.AppHost
```

This starts the Aspire dashboard and launches Postgres, the API, and the Next.js dev server.
Open the dashboard URL printed in the console to see all resources and their endpoints.
(`npm install` for the web app is handled automatically by `.WithNpm()`.)

## Run pieces individually (without Aspire)

```bash
# API
dotnet run --project src/HomeProjectManagement.ApiService

# Web (set API_BASE_URL to the API's URL)
cd src/web && npm install && npm run dev
```
