# Building & publishing container images

How to containerize this application and publish the images that the homelab
k3s cluster runs. This is the **app-repo half** of the deployment story: it owns
the Dockerfiles and the image build/push flow. The **cluster half** — Kubernetes
manifests, the PostgreSQL database, Flux wiring, DNS and exposure — lives in the
`home-lab-infra` repository:

> See `home-lab-infra/docs/runbooks/deploy-home-project-management.md` for the
> full deployment runbook.

## Background: why Dockerfiles at all

Locally, the whole system is orchestrated by the **Aspire AppHost**
(`src/HomeProjectManagement.AppHost/AppHost.cs`). Aspire is a **local-dev
orchestrator only** — it is not a deployment target. It launches Postgres, the
API and the Next.js dev server on your machine and wires them together with
environment variables. None of that runs in the cluster.

To run in k3s we ship the two long-lived workloads as plain container images and
let Kubernetes manifests replace Aspire's wiring:

| Aspire (local) | Cluster (k3s) |
|---|---|
| `AddPostgres(...).WithDataVolume()` | CloudNativePG `Cluster` + PVC |
| `AddProject<…ApiService>(...)` | `api` Deployment from the API image |
| `AddNextJsApp("web", …)` | `web` Deployment from the web image |
| `.WithReference(...)` / `.WaitFor(...)` | Service DNS + readiness probes |
| `WithEnvironment("API_BASE_URL", …)` | env var on the `web` container |

The `AppHost` and `ServiceDefaults` projects are **not** containerized — they are
dev-time/shared concerns. We build images for exactly two projects:

- **API** — `HomeProjectManagement.ApiService` (ASP.NET, .NET 10)
- **Web** — `src/web` (Next.js 16, `output: "standalone"` already set in
  `next.config.ts`)

## Images

Published to the in-cluster registry (same one `bvb-analyzer` uses):

| Component | Image | Container port |
|---|---|---|
| API | `registry.crozy.eu/home-project-management/api` | `8080` |
| Web | `registry.crozy.eu/home-project-management/web` | `3000` |

Tag with an explicit semantic version per release (e.g. `v0.1.0`) — **never rely
on `latest`**. The deployment manifests pin an exact tag, and a new release means
building a new tag and bumping it in the overlay (see the runbook). This mirrors
the `bvb-analyzer:v1.0.2` convention already in the cluster.

## The runtime contract these images honor

The manifests inject configuration through environment variables. The images must
keep honoring this contract:

### API container

| Env var | Purpose | Example |
|---|---|---|
| `ConnectionStrings__projectsdb` | Npgsql connection string. `Program.cs` reads it via `builder.AddNpgsqlDbContext<AppDbContext>("projectsdb")`. | `Host=hpm-db-rw;Port=5432;Database=projectsdb;Username=hpm;Password=…;SSL Mode=Require;Trust Server Certificate=true` |
| `ASPNETCORE_ENVIRONMENT` | Runs as `Production` in-cluster. | `Production` |
| `ASPNETCORE_HTTP_PORTS` | Kestrel listen port (defaults to `8080` in the aspnet base image). | `8080` |

Two consequences of running as `Production` that the cluster side relies on:

- **EF Core migrations** are applied automatically on startup
  (`db.Database.Migrate()` in `Program.cs`). No separate migration Job is needed;
  the DB just has to be reachable when the API starts.
- **Health endpoints `/health` and `/alive` are NOT mapped in Production** —
  `ServiceDefaults` only maps them when `IsDevelopment()`. The cluster therefore
  uses **TCP probes** against port `8080`, not HTTP health checks. (If you later
  want real HTTP probes, expose the health endpoints outside Development in
  `ServiceDefaults/Extensions.cs`.)

### Web container

| Env var | Purpose | Example |
|---|---|---|
| `API_BASE_URL` | Backend base URL, read in `app/lib/api.ts`. | `http://api:8080` |
| `PORT` / `HOSTNAME` | Next standalone listen address. | `3000` / `0.0.0.0` |
| `NODE_ENV` | `production`. | `production` |

All frontend data access is **server-side** (Server Components + Server Actions in
`app/actions.ts`), so the browser never calls `API_BASE_URL` directly. That means
`API_BASE_URL` points at the **in-cluster** API service (`http://api:8080`), and
**the API needs no ingress** — only `web` is exposed.

## Dockerfiles

Create these two files (and the two `.dockerignore` files) in the repo. The API
builds from the **repo root** (it spans multiple projects); the web builds from
**`src/web`**.

### `src/HomeProjectManagement.ApiService/Dockerfile`

```dockerfile
# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Solution-wide build config first, for better layer caching. (Central Package
# Management + shared MSBuild props are required by every project's restore.)
COPY Directory.Build.props Directory.Packages.props ./

# Restore against just the csproj graph the API needs.
COPY src/HomeProjectManagement.ApiService/*.csproj      src/HomeProjectManagement.ApiService/
COPY src/HomeProjectManagement.Application/*.csproj      src/HomeProjectManagement.Application/
COPY src/HomeProjectManagement.Domain/*.csproj          src/HomeProjectManagement.Domain/
COPY src/HomeProjectManagement.Infrastructure/*.csproj  src/HomeProjectManagement.Infrastructure/
COPY src/HomeProjectManagement.ServiceDefaults/*.csproj src/HomeProjectManagement.ServiceDefaults/
RUN dotnet restore src/HomeProjectManagement.ApiService/HomeProjectManagement.ApiService.csproj

# Copy sources and publish.
COPY src/ src/
RUN dotnet publish src/HomeProjectManagement.ApiService/HomeProjectManagement.ApiService.csproj \
    -c Release -o /app --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Run as the non-root user provided by the base image.
USER $APP_UID
ENTRYPOINT ["dotnet", "HomeProjectManagement.ApiService.dll"]
```

### `.dockerignore` (repo root — for the API build)

```gitignore
**/bin/
**/obj/
**/node_modules/
**/.next/
.git/
.vs/
.vscode/
.claude/
**/*.user
```

### `src/web/Dockerfile`

```dockerfile
# syntax=docker/dockerfile:1

# ---- Dependencies ----
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

# ---- Build ----
FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

# ---- Runtime ----
FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
RUN addgroup -g 1001 nodejs && adduser -u 1001 -G nodejs -S nextjs
# Next.js standalone output: server.js + minimal node_modules, plus static assets.
COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
USER nextjs
EXPOSE 3000
CMD ["node", "server.js"]
```

> **Next.js 16 caveat.** Per `src/web/AGENTS.md`, this is not the Next.js in your
> training data. Before changing the web Dockerfile, confirm the standalone output
> layout in `src/web/node_modules/next/dist/docs/`. The `output: "standalone"`
> contract (a `server.js` plus `.next/static` and `public`) is what the runtime
> stage above depends on.

### `src/web/.dockerignore`

```gitignore
node_modules/
.next/
.git/
npm-debug.log
```

## Build & push

The cluster registry requires auth (the `regcred` pull secret handles *pulling*
in-cluster; *pushing* from your workstation needs a `docker login`). Build for
`linux/amd64` (the cluster nodes' architecture) — relevant if you build on an
ARM Mac.

```bash
# One-time: authenticate to the registry.
docker login registry.crozy.eu

VERSION=v0.1.0

# API — built from the repo root.
docker build --platform linux/amd64 \
  -f src/HomeProjectManagement.ApiService/Dockerfile \
  -t registry.crozy.eu/home-project-management/api:$VERSION .
docker push registry.crozy.eu/home-project-management/api:$VERSION

# Web — built from src/web.
docker build --platform linux/amd64 \
  -t registry.crozy.eu/home-project-management/web:$VERSION src/web
docker push registry.crozy.eu/home-project-management/web:$VERSION
```

Then bump the two image tags in the cluster overlay and let Flux roll it out —
see the runbook in `home-lab-infra`.

## Release checklist

1. Merge the application change to `main`.
2. Pick the next `VERSION` (semantic, e.g. `v0.2.0`).
3. Build & push both images (commands above).
4. In `home-lab-infra`, bump both tags in
   `k3s/apps/base/home-project-management/**` (or the relevant overlay) and open
   the DEV PR; validate; then the PROD PR. (Two-PR dev→prod rule — see runbook.)
