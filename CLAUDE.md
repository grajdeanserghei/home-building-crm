# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Home Project Management: a .NET Aspire application orchestrating a minimal-API backend, a PostgreSQL database, and a Next.js frontend. The single domain entity is `Project` (name, description, status, due date) with full CRUD.

**Purpose:** a private, internal tool for the four people building a duplex together (the owner, the owner's spouse, and two friends) to track and coordinate the build. It is internal-only and must require authentication — access is restricted to those four stakeholders; there is no open sign-up. See [`docs/project-overview.md`](docs/project-overview.md).

**Architecture mandate:** the backend must follow **Domain-Driven Design** and **Hexagonal architecture** (ports and adapters), keeping the domain core isolated from infrastructure. This is a hard requirement and the target direction — the current minimal-API/EF-Core code (described below) does **not** yet follow it, so expect restructuring rather than assuming the mandate is already implemented.

## Architecture

The system is wired together by the **Aspire AppHost** (`src/HomeProjectManagement.AppHost/AppHost.cs`), which is the entry point that launches everything. Understanding the dependency chain there is the key to the whole project:

- **postgres** (with a persistent data volume + pgAdmin UI) → exposes database `projectsdb`.
- **apiservice** (`HomeProjectManagement.ApiService`) → references `projectsdb`, waits for it.
- **web** (Next.js, in `src/web`) → references apiservice, waits for it. The AppHost injects the backend URL into the frontend via the `API_BASE_URL` environment variable.

Resource names in `AppHost.cs` are contractual: `"projectsdb"` must match the connection name in `Program.cs` (`builder.AddNpgsqlDbContext<AppDbContext>("projectsdb")`), and `API_BASE_URL` must match what the frontend reads in `app/lib/api.ts`. Renaming a resource without updating its consumers breaks service discovery.

### Backend (`src/HomeProjectManagement.ApiService`)
- Minimal API; all endpoints are defined inline in `Program.cs` under the `/api/projects` route group. There are no controllers.
- EF Core via Npgsql. `AppDbContext` (Data/) + `Project`/`ProjectStatus` (Models/). `ProjectStatus` is persisted as a string (`HasConversion<string>`).
- **Schema is created with `EnsureCreated()` in Development only** — there are no EF migrations yet. For production schema changes, introduce migrations rather than relying on `EnsureCreated`.
- CORS is wide open (`AllowAnyOrigin`) to permit the browser-side Next.js dev server to call the API.

### Frontend (`src/web`)
- Next.js App Router. Data fetching lives in `app/lib/api.ts`; mutations are React Server Actions in `app/actions.ts` (which `revalidatePath("/")` after writes).
- The `Project`/`ProjectStatus` TypeScript types in `api.ts` mirror the C# model — keep them in sync when the backend model changes.
- `API_BASE_URL` falls back to `http://localhost:5000` when running standalone outside Aspire.

## Commands

Run all commands from the repository root unless noted.

- **Run the whole app (normal dev workflow):** `dotnet run --project src/HomeProjectManagement.AppHost` — starts Postgres, the API, the Next.js dev server (`npm run dev` is invoked automatically by Aspire), and the Aspire dashboard. Do not start the frontend or API separately for normal work.
- **Build the .NET solution:** `dotnet build`
- **Frontend lint:** `cd src/web && npm run lint`
- **Frontend production build:** `cd src/web && npm run build`
- **Frontend deps:** `cd src/web && npm install`

There is no test suite at present.

## NuGet packages — central management required

All NuGet packages **must** be managed centrally via [Central Package Management (CPM)](https://learn.microsoft.com/nuget/consume-packages/central-package-management). Package versions live in a single `Directory.Packages.props` at the repository root; individual `.csproj` files declare `<PackageReference Include="..." />` **without** a `Version` attribute. Do not pin or override versions per-project. When adding a dependency, add or update its `<PackageVersion>` entry in `Directory.Packages.props` and reference it (version-less) from the project that needs it. See [`docs/guides/central-package-management.md`](docs/guides/central-package-management.md).

## Next.js version warning

`src/web` uses Next.js 16, which has breaking changes from earlier versions. Per `src/web/AGENTS.md`: read the relevant guide in `src/web/node_modules/next/dist/docs/` before writing frontend code, and heed deprecation notices — do not assume older Next.js conventions.

## Documentation

`docs/` holds specs (`specifications/`, start from `_template.md`), architecture notes (`architecture/`), and guides (`guides/`). Use kebab-case filenames, one document per topic.
