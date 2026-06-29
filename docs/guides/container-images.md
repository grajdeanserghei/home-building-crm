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
| `AddProject<…McpServer>(...).WithExternalHttpEndpoints()` | `mcp` Deployment + ingress from the MCP image |
| `AddNextJsApp("web", …)` | `web` Deployment from the web image |
| `.WithReference(...)` / `.WaitFor(...)` | Service DNS + readiness probes |
| `WithEnvironment("API_BASE_URL", …)` | env var on the `web` container |

The `AppHost` and `ServiceDefaults` projects are **not** containerized — they are
dev-time/shared concerns. We build images for exactly three long-lived workloads:

- **API** — `HomeProjectManagement.ApiService` (ASP.NET, .NET 10)
- **MCP** — `HomeProjectManagement.McpServer` (ASP.NET, .NET 10) — a second
  driving adapter that lets remote AI agents (Claude / ChatGPT) drive data entry
  over Streamable HTTP. It shares `projectsdb` with the API but, unlike the API,
  is reached **directly from outside the cluster**, so it gets its own ingress.
- **Web** — `src/web` (Next.js 16, `output: "standalone"` already set in
  `next.config.ts`)

## Images

Published to the in-cluster registry (same one `bvb-analyzer` uses):

| Component | Image | Container port |
|---|---|---|
| API | `registry.crozy.eu/home-project-management/api` | `8080` |
| MCP | `registry.crozy.eu/home-project-management/mcp` | `8080` |
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

### MCP container

The MCP server uses the same connection-string and port contract as the API:

| Env var | Purpose | Example |
|---|---|---|
| `ConnectionStrings__projectsdb` | Npgsql connection string. `Program.cs` reads it via `builder.AddNpgsqlDbContext<AppDbContext>("projectsdb")`. | `Host=hpm-db-rw;Port=5432;Database=projectsdb;Username=hpm;Password=…;SSL Mode=Require;Trust Server Certificate=true` |
| `ASPNETCORE_ENVIRONMENT` | Runs as `Production` in-cluster. | `Production` |
| `ASPNETCORE_HTTP_PORTS` | Kestrel listen port (defaults to `8080` in the aspnet base image). | `8080` |

Two things differ from the API and the cluster side must honor them:

- **It does NOT migrate.** Schema is owned by the API's startup
  `db.Database.Migrate()`; the MCP host only reads/writes the same database.
  Order the rollout so the API has migrated before (or alongside) the MCP
  Deployment, and never run two migrators.
- **It is exposed — behind Cloudflare Access.** Remote agent clients connect to it
  directly, so `mcp` is reachable from the internet (the API is not). That public
  reachability is provided by a **Cloudflare Tunnel**, and the OAuth flow + Google
  login + stakeholder allow-list are enforced at the edge by **Cloudflare Access
  (Managed OAuth)**. The origin only **validates the forwarded assertion**, turned
  on in-cluster via the `CloudflareAccess` section — supplied as environment
  variables, never committed:

  | Env var | Purpose | Example |
  |---|---|---|
  | `CloudflareAccess__Enabled` | `true` in-cluster — turns on origin-side validation of the Access assertion + the stakeholder allow-list re-check. Defaults to `false` (the network-restricted local-dev posture). | `true` |
  | `CloudflareAccess__TeamDomain` | The Access team domain — the token issuer (`iss`) and the base for the signing keys at `{TeamDomain}/cdn-cgi/access/certs`. | `https://crozy.cloudflareaccess.com` |
  | `CloudflareAccess__Audience` | The Access application's Audience (AUD) tag — the `aud` claim the assertion carries. | `a1b2c3…` (the app's AUD tag) |
  | `CloudflareAccess__AllowedEmails__0`, `__1`, … | Defense-in-depth allow-list re-check (the Access policy at the edge is the primary gate). Empty trusts the edge policy alone. | `someone@example.com` |

  Startup throws if `CloudflareAccess__Enabled=true` but `TeamDomain`/`Audience` are
  unset, so a misconfigured exposed server fails fast rather than running open. The
  Tunnel + Access application + policy live in the `home-lab-infra` repo / Cloudflare
  Zero Trust dashboard — see [`cloudflare-access-authentication.md`](../specifications/cloudflare-access-authentication.md).

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

`web` is exposed through the **Cloudflare Tunnel** and gated by a **Cloudflare Access**
application (browser Google login + the stakeholder email allow-list), so unauthenticated
requests never reach the Next.js server. No auth code runs in `web` for gating — see
[`cloudflare-access-authentication.md`](../specifications/cloudflare-access-authentication.md).

## Dockerfiles

Create these three Dockerfiles (and the two `.dockerignore` files) in the repo.
The API and the MCP server both build from the **repo root** (they span multiple
projects and share the root `.dockerignore`); the web builds from **`src/web`**.

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

### `src/HomeProjectManagement.McpServer/Dockerfile`

Identical shape to the API Dockerfile — it builds from the repo root and restores
the same Application/Domain/Infrastructure/ServiceDefaults graph (plus the
McpServer csproj). Only the project paths and the entrypoint dll differ.

```dockerfile
# syntax=docker/dockerfile:1

# ---- Build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Solution-wide build config first, for better layer caching. (Central Package
# Management + shared MSBuild props are required by every project's restore.)
COPY Directory.Build.props Directory.Packages.props ./

# Restore against just the csproj graph the MCP server needs.
COPY src/HomeProjectManagement.McpServer/*.csproj       src/HomeProjectManagement.McpServer/
COPY src/HomeProjectManagement.Application/*.csproj      src/HomeProjectManagement.Application/
COPY src/HomeProjectManagement.Domain/*.csproj          src/HomeProjectManagement.Domain/
COPY src/HomeProjectManagement.Infrastructure/*.csproj  src/HomeProjectManagement.Infrastructure/
COPY src/HomeProjectManagement.ServiceDefaults/*.csproj src/HomeProjectManagement.ServiceDefaults/
RUN dotnet restore src/HomeProjectManagement.McpServer/HomeProjectManagement.McpServer.csproj

# Copy sources and publish.
COPY src/ src/
RUN dotnet publish src/HomeProjectManagement.McpServer/HomeProjectManagement.McpServer.csproj \
    -c Release -o /app --no-restore

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
# Run as the non-root user provided by the base image.
USER $APP_UID
ENTRYPOINT ["dotnet", "HomeProjectManagement.McpServer.dll"]
```

### `.dockerignore` (repo root — shared by the API and MCP builds)

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

# MCP — also built from the repo root.
docker build --platform linux/amd64 \
  -f src/HomeProjectManagement.McpServer/Dockerfile \
  -t registry.crozy.eu/home-project-management/mcp:$VERSION .
docker push registry.crozy.eu/home-project-management/mcp:$VERSION

# Web — built from src/web.
docker build --platform linux/amd64 \
  -t registry.crozy.eu/home-project-management/web:$VERSION src/web
docker push registry.crozy.eu/home-project-management/web:$VERSION
```

Then bump the three image tags in the cluster overlay and let Flux roll it out —
see the runbook in `home-lab-infra`.

## Release checklist

1. Merge the application change to `main`.
2. Pick the next `VERSION` (semantic, e.g. `v0.2.0`).
3. Build & push all three images (commands above).
4. In `home-lab-infra`, bump all three tags in
   `k3s/apps/base/home-project-management/**` (or the relevant overlay) and open
   the DEV PR; validate; then the PROD PR. (Two-PR dev→prod rule — see runbook.)
