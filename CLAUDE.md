# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Home Project Management: a .NET Aspire application orchestrating a backend, a PostgreSQL database, and a Next.js frontend. The domain models a construction-bidding process — its aggregates are `Project`, `WorkPackage`, `Contractor`, `Bid` (with an owned discussion-note log), and `UnitOfMeasure`, with `BillOfQuantities` and `Contract` planned (their ids are already referenced). See [`docs/architecture/domain-model.md`](docs/architecture/domain-model.md) for the full model and ubiquitous language.

**Purpose:** a private, internal tool for the four people building a duplex together (the owner, the owner's spouse, and two friends) to track and coordinate the build. It is internal-only and must require authentication — access is restricted to those four stakeholders; there is no open sign-up. See [`docs/project-overview.md`](docs/project-overview.md).

**Architecture mandate:** the backend follows **Domain-Driven Design** and **Hexagonal architecture** (ports and adapters), keeping the domain core isolated from infrastructure. This is a hard requirement and is now implemented as the layered structure below — preserve it when adding features rather than collapsing logic back into the host. See [`docs/architecture/hexagonal-architecture.md`](docs/architecture/hexagonal-architecture.md).

## Architecture

The system is wired together by the **Aspire AppHost** (`src/HomeProjectManagement.AppHost/AppHost.cs`), which is the entry point that launches everything. Understanding the dependency chain there is the key to the whole project:

- **postgres** (with a persistent data volume + pgAdmin UI) → exposes database `projectsdb`.
- **apiservice** (`HomeProjectManagement.ApiService`) → references `projectsdb`, waits for it.
- **web** (Next.js, in `src/web`) → references apiservice, waits for it. The AppHost injects the backend URL into the frontend via the `API_BASE_URL` environment variable.

Resource names in `AppHost.cs` are contractual: `"projectsdb"` must match the connection name in `Program.cs` (`builder.AddNpgsqlDbContext<AppDbContext>("projectsdb")`), and `API_BASE_URL` must match what the frontend reads in `app/lib/api.ts`. Renaming a resource without updating its consumers breaks service discovery.

### Backend — hexagonal layering

The backend is split into four projects, one per hexagonal ring. Dependencies point inward only (Domain depends on nothing; the host depends on everything):

- **`HomeProjectManagement.Domain`** — the isolated core. One folder per aggregate (`Projects/`, `WorkPackages/`, `Contractors/`, `Bids/`, `UnitsOfMeasure/`), each with its aggregate root, a strongly-typed id (`readonly record struct XId(Guid Value) : IStronglyTypedId`), enums, an `Events/` folder of `IDomainEvent` records, and an `I…Repository` port. `Common/` holds the base classes (`AggregateRoot<TId>`, `Entity<TId>`, `ValueObject`) and shared value objects (`Money`, `Address`, `ContactInfo`, `UserId`, …). Aggregates reference each other **only by id**, never by holding the other object. Construction goes through a static factory; state changes go through intention-revealing methods that enforce invariants and `Raise(...)` events. **No clock and no I/O in the domain** — callers pass `now` (a `DateTimeOffset`) in.
- **`HomeProjectManagement.Application`** — use cases (driving ports). One folder per aggregate with DTOs/commands, an `I…AppService` port, and its implementation. Services are thin: load via a repository port, invoke domain behaviour, commit via `IUnitOfWork`. Cross-aggregate guards live here (e.g. checking a parent exists, returning `null` → 404). `Abstractions/` holds driven ports the domain/app needs (`ICurrentUser`, `IExchangeRateProvider`, `IDomainEventDispatcher`). Registered via `AddApplication()`.
- **`HomeProjectManagement.Infrastructure`** — driven adapters. `Persistence/` has `AppDbContext` (one `DbSet` per aggregate root only), one `IEntityTypeConfiguration` per aggregate under `Configurations/`, repository implementations under `Repositories/`, the `UnitOfWork` (stamps audit fields from `ICurrentUser`/`TimeProvider`, then dispatches domain events post-commit), and EF `Migrations/`. Strongly-typed ids map to `Guid` columns automatically via the convention in `Conversions/` — no per-id config. Other adapters: `Identity/StubCurrentUser` (fixed `UserId` until real auth), `ExchangeRates/`, `Events/`. Registered via `AddInfrastructure()`.
- **`HomeProjectManagement.ApiService`** — the driving (HTTP) adapter and composition root. `Program.cs` wires `AddApplication()` + `AddInfrastructure()` and maps endpoint groups; the actual endpoints are thin minimal-API handlers in `Endpoints/*.cs` (one file per aggregate, e.g. `BidEndpoints.cs`) that call an app-service port and return DTOs. The host never touches EF Core or the domain directly.

Other backend facts:
- **EF Core migrations are the source of truth for schema.** `Program.cs` runs `db.Database.Migrate()` on startup. Add a migration after any model/configuration change (see Commands) — do **not** use `EnsureCreated`.
- Enums are persisted as their string names (`HasConversion<string>`) and serialized as strings over JSON (a `JsonStringEnumConverter` in `Program.cs`) so they match the frontend's TypeScript types.
- CORS is wide open (`AllowAnyOrigin`) to permit the browser-side Next.js dev server to call the API. Real authentication for the four stakeholders is mandated but not yet wired (tracked in `docs/specifications/`).

### Frontend (`src/web`)
- Next.js App Router. Data fetching lives in `app/lib/api.ts`; mutations are React Server Actions in `app/actions.ts` (which `revalidatePath("/")` after writes).
- The `Project`/`ProjectStatus` TypeScript types in `api.ts` mirror the C# model — keep them in sync when the backend model changes.
- `API_BASE_URL` falls back to `http://localhost:5000` when running standalone outside Aspire.

## Commands

Run all commands from the repository root unless noted.

- **Run the whole app (normal dev workflow):** `dotnet run --project src/HomeProjectManagement.AppHost` — starts Postgres, the API, the Next.js dev server (`npm run dev` is invoked automatically by Aspire), and the Aspire dashboard. Do not start the frontend or API separately for normal work.
- **Build the .NET solution:** `dotnet build`
- **Add an EF Core migration** (after any change to an aggregate or its `IEntityTypeConfiguration`): use **Infrastructure as both the project and the startup project** — the `ApiService` host does not reference `Microsoft.EntityFrameworkCore.Design`, so it cannot drive the tooling; Infrastructure carries the Design package and a `DesignTimeDbContextFactory`:
  ```
  dotnet ef migrations add <Name> --project src/HomeProjectManagement.Infrastructure --startup-project src/HomeProjectManagement.Infrastructure
  ```
  Migrations are applied automatically at host startup via `db.Database.Migrate()`.
- **Frontend lint:** `cd src/web && npm run lint`
- **Frontend production build:** `cd src/web && npm run build`
- **Frontend deps:** `cd src/web && npm install`

There is no test suite at present.

## NuGet packages — central management required

All NuGet packages **must** be managed centrally via [Central Package Management (CPM)](https://learn.microsoft.com/nuget/consume-packages/central-package-management). Package versions live in a single `Directory.Packages.props` at the repository root; individual `.csproj` files declare `<PackageReference Include="..." />` **without** a `Version` attribute. Do not pin or override versions per-project. When adding a dependency, add or update its `<PackageVersion>` entry in `Directory.Packages.props` and reference it (version-less) from the project that needs it. See [`docs/guides/central-package-management.md`](docs/guides/central-package-management.md).

## Shared MSBuild properties

Common project properties are centralized in `Directory.Build.props` at the repository root and apply to **every** project automatically: `TargetFramework` (the .NET version — currently `net10.0`), `Nullable`, and `ImplicitUsings`. Do not redeclare these in individual `.csproj` files; change the .NET version in one place (`Directory.Build.props`) to move the whole solution. Only genuinely project-specific properties belong in a `.csproj` (e.g. `OutputType`, `UserSecretsId`, `IsAspireSharedProject`, `NoWarn`).

## Next.js version warning

`src/web` uses Next.js 16, which has breaking changes from earlier versions. Per `src/web/AGENTS.md`: read the relevant guide in `src/web/node_modules/next/dist/docs/` before writing frontend code, and heed deprecation notices — do not assume older Next.js conventions.

## Documentation

`docs/` holds specs (`specifications/`, start from `_template.md`), architecture notes (`architecture/`), and guides (`guides/`). Use kebab-case filenames, one document per topic.
