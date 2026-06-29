# Authentication via Cloudflare Access (web UI + MCP server)

- **Status:** Approved <!-- Draft | In Review | Approved | Implemented | Deprecated -->
- **Author:** Serghei Grajdean
- **Date:** 2026-06-26
- **Related:** [`remote-mcp-server.md`](remote-mcp-server.md), [`docs/project-overview.md`](../project-overview.md), [`docs/guides/container-images.md`](../guides/container-images.md), [`docs/architecture/hexagonal-architecture.md`](../architecture/hexagonal-architecture.md)

## Summary

Authenticate every entry point to the application with **Cloudflare Access (Zero Trust)**, fronted by a **Cloudflare Tunnel**, federating login to **Google** and restricting access to the four stakeholders via a single email allow-list. One mechanism gates both the human **web UI** and the **remote MCP server**; the **API** and **Postgres** stay private (no ingress). This supersedes the Microsoft Entra External ID design previously sketched in [`remote-mcp-server.md`](remote-mcp-server.md).

## Motivation

The project mandate (see [`project-overview.md`](../project-overview.md) and `CLAUDE.md`) is that the tool **requires authentication** and is restricted to four named stakeholders with **no open sign-up**. Until now nothing was wired: the web UI had no auth at all, the API used a `StubCurrentUser`, and the MCP server carried a config-gated, never-enabled Entra implementation.

The deployment already standardises on a **Cloudflare Tunnel** to expose the homelab k3s cluster. Putting **Cloudflare Access** in front of that tunnel gives us, with almost no application code:

- **Gmail login** — Google as the IdP, one-click in Access.
- **"Only specific members"** — an Access policy `Emails` allow-list, enforced at the edge.
- **One mechanism for both surfaces** — the same Access layer protects the browser app and the MCP server (the latter via **Managed OAuth**, which makes Access a standard OAuth 2.0 authorization server for MCP clients).

Goals:

- [x] Unauthenticated requests never reach the `web` or `mcp` containers.
- [x] Both surfaces authenticate the same four Gmail accounts from a single allow-list.
- [x] The MCP server keeps working with Claude / ChatGPT remote-MCP clients (OAuth 2.1 + PKCE).
- [x] Retire the Entra External ID design; no second identity system to run.

### Non-goals

- **Gating code inside the app.** Access authenticates at the edge; `web` needs no `middleware.ts`, NextAuth, or session layer for gating.
- **Protecting the API with its own Access app.** The API has no ingress and is reachable only in-cluster by `web` (itself behind Access); network isolation is its boundary.
- **Real per-user audit identity for the web UI.** Propagating the signed-in stakeholder into the API's audit fields (`createdBy`/`modifiedBy`) is a tracked follow-up (see [Follow-up](#follow-up)). The MCP server *does* attribute writes to the authenticated principal today.
- **Service-token (non-interactive) MCP access.** Humans use Google login; machine tokens carry no `email` and would fail the allow-list. Out of scope until needed.

## Design

### Topology

```
Internet ──HTTPS──> Cloudflare edge (Access policies · Google IdP · email allow-list)
                          │
                     Cloudflare Tunnel (cloudflared)
   hpm.crozy.eu     ───── Access app "hpm-web"  ──> web container :3000   (browser login)
   mcp.hpm.crozy.eu ───── Access app "hpm-mcp"  ──> mcp container :8080   (Managed OAuth)
                                                    │
                          (private, no ingress) ────┼──> api container :8080
                                                    └──> postgres
```

**Public hostnames (created):**

| Hostname | Access application | Origin (k3s service) | Auth model |
|---|---|---|---|
| `hpm.crozy.eu` | the web UI | `web` `:3000` | Browser Google login |
| `mcp.hpm.crozy.eu` | the MCP server | `mcp` `:8080` | Managed OAuth (OAuth 2.1 + PKCE) |

The **API** and **Postgres** have no public hostname and no Tunnel ingress — they are reached only in-cluster (`web` → `api:8080`).

- Both Access applications forward a signed **`Cf-Access-Jwt-Assertion`** (also a `CF_Authorization` cookie) carrying the verified email to their origin.
- Token validation contract: **issuer** (`iss`) = the team domain `https://ozius.cloudflareaccess.com`; **signing keys** (JWKS) at `https://ozius.cloudflareaccess.com/cdn-cgi/access/certs` (raw JWKS; Cloudflare keeps the current + previous key live); **audience** (`aud`) = each application's unique **AUD tag**.
- The four stakeholders' Gmail addresses live in **one Access policy `Emails` allow-list** — the single source of truth.

### web — browser login, no app code

Cloudflare Access authenticates the browser (Google) before any request reaches Next.js. An unlisted account is denied at the edge. `src/web` is unchanged: all data access is already server-side, and the browser never calls the API directly.

### mcp — Managed OAuth at the edge, validate-only at the origin

With **Managed OAuth** enabled on the `mcp` Access application, Cloudflare becomes the OAuth 2.0 authorization server for MCP clients: it answers the client's `401 + WWW-Authenticate`, serves OAuth/protected-resource discovery (RFC 8414 / 9728 / 8707), supports Dynamic Client Registration, runs Authorization Code + PKCE, and federates login to Google. After the edge authenticates and authorises the client, it forwards the request to the origin with the `Cf-Access-Jwt-Assertion`.

The origin (`HomeProjectManagement.McpServer`) therefore does **not** publish metadata or run the challenge flow. It only:

1. **Validates the assertion** — `AddJwtBearer` with issuer/audience/lifetime/signature checks; a `JwtBearerEvents.OnMessageReceived` hook reads the token from the `Cf-Access-Jwt-Assertion` header / `CF_Authorization` cookie (Cloudflare does not use `Authorization: Bearer`); the signing keys come from the team's JWKS via a small `ConfigurationManager` retriever (`CloudflareCertsRetriever`).
2. **Re-checks the allow-list** — the `Stakeholders` authorization policy requires an authenticated user and (defense-in-depth) a verified email in `CloudflareAccess:AllowedEmails`.
3. **Attributes writes** — `PrincipalCurrentUser` maps the assertion's `sub` (a stable Access UUID) or `email` to a `UserId`, replacing `StubCurrentUser`. This is a pure Infrastructure adapter swap behind the existing `ICurrentUser` seam.

All of this is gated by `CloudflareAccess:Enabled` and ships **off** so local dev stays network-restricted with the stub. Code: `src/HomeProjectManagement.McpServer/Authentication/CloudflareAccess{Options,AuthExtensions}.cs`, `Identity/PrincipalCurrentUser.cs`, wired in `Program.cs`.

### Configuration

Supplied via user-secrets / environment, never committed (section `CloudflareAccess`):

| Key | Meaning | Example |
|---|---|---|
| `Enabled` | Turns on origin-side validation + the allow-list re-check. `false` in local dev. | `true` |
| `TeamDomain` | The Zero Trust team domain — the issuer and JWKS base. | `https://ozius.cloudflareaccess.com` |
| `Audience` | The `hpm-mcp` (`mcp.hpm.crozy.eu`) application's AUD tag. | `a1b2c3…` |
| `AllowedEmails__0,1,…` | Defense-in-depth allow-list (the edge Access policy is the primary gate). | `someone@gmail.com` |

Container env keys use the `CloudflareAccess__*` form — see [`container-images.md`](../guides/container-images.md).

### Cloudflare setup procedure (for `home-lab-infra`)

Executed in the Cloudflare Zero Trust dashboard + the `home-lab-infra` repo. Concrete hostnames: **`hpm.crozy.eu`** (web) and **`mcp.hpm.crozy.eu`** (MCP). The Cloudflare Zero Trust team is **`ozius`**, so the team domain is **`https://ozius.cloudflareaccess.com`**.

**1. Google as the identity provider (one-time, account-wide).**
- Google Cloud Console → **APIs & Services → Credentials**. Configure the **OAuth consent screen** with audience type **External** (lets personal Gmail accounts sign in; no Workspace required), and either publish it or add the four stakeholders as test users.
- Create an **OAuth client ID**, type **Web application**, with:
  - Authorized JavaScript origin: `https://ozius.cloudflareaccess.com`
  - Authorized redirect URI: `https://ozius.cloudflareaccess.com/cdn-cgi/access/callback`
- In Zero Trust → **Settings → Authentication → Login methods → Add new → Google**: paste the **Client ID** (Cloudflare's "App ID") and **Client Secret**; enable PKCE. Use **Test** to confirm a Gmail round-trip.

**2. Tunnel + DNS (`home-lab-infra`).** A Cloudflare Tunnel (`cloudflared`) with ingress mapping the two public hostnames to the in-cluster services; proxied DNS (CNAME) records for `hpm.crozy.eu` and `mcp.hpm.crozy.eu` pointing at the tunnel. No ingress for the API or Postgres. Ingress rules (service names match the cluster's k8s Services — see [`container-images.md`](../guides/container-images.md)):

```yaml
ingress:
  - hostname: hpm.crozy.eu
    service: http://web:3000
  - hostname: mcp.hpm.crozy.eu
    service: http://mcp:8080
  - service: http_status:404      # everything else is rejected
```

**3. Reusable allow-list group.** Zero Trust → **Access → Access Groups** → create one group (e.g. `hpm-stakeholders`) whose **Emails** are the four stakeholders' Gmail addresses. Both apps reference this group, so the allow-list has a single source of truth.

**4. Access application `hpm-web`** (already created → `hpm.crozy.eu`).
- Type: **Self-hosted**, application domain `hpm.crozy.eu`.
- Identity providers: **Google** (optionally disable One-Time PIN to make Google the only method).
- Policy: **Allow**, selector = the `hpm-stakeholders` group.

**5. Access application `hpm-mcp`** (already created → `mcp.hpm.crozy.eu`).
- Type: **Self-hosted**, application domain `mcp.hpm.crozy.eu`.
- Enable **Managed OAuth** (turns Access into the OAuth 2.0 authorization server for MCP clients): allow **localhost** and **loopback** redirect clients, and add the redirect URIs the Claude/ChatGPT desktop clients use.
- Identity providers: **Google**.
- Policy: **Allow**, selector = the same `hpm-stakeholders` group.

**6. Hand the backend its config.** From each application's settings copy the **AUD tag**, and note the team domain. These populate the deployed `CloudflareAccess__*` env (per [Configuration](#configuration) / [`container-images.md`](../guides/container-images.md)) and flip `CloudflareAccess__Enabled=true` on the `mcp` Deployment:

| Value | Source | Backend key (mcp Deployment) |
|---|---|---|
| `https://ozius.cloudflareaccess.com` | team domain | `CloudflareAccess__TeamDomain` |
| `hpm-mcp` app **AUD tag** | the `mcp.hpm.crozy.eu` application | `CloudflareAccess__Audience` |
| the four Gmail addresses | the `hpm-stakeholders` group | `CloudflareAccess__AllowedEmails__0..3` |

(The `hpm-web` AUD tag is only needed once the **web UI** validates the assertion itself — the deferred per-user-audit [follow-up](#follow-up); web gating needs no app config today.)

**7. Update the deploy runbook** in `home-lab-infra` (`docs/runbooks/deploy-home-project-management.md`) to reflect the two hostnames, the tunnel ingress, and the `mcp` auth env.

### Shared-code note

The auth building blocks currently live in the `HomeProjectManagement.McpServer` project, because only the MCP server is authenticated in this pass. When the API later adopts the same validation (the follow-up below), lift `CloudflareAccessOptions`, `CloudflareAccessAuthExtensions`, and `PrincipalCurrentUser` into `Infrastructure` (or a small `*.Auth` library) rather than duplicating them.

## Follow-up

**Real per-user audit identity for the web UI.** Have `web` read `Cf-Access-Jwt-Assertion` server-side (via `next/headers`), forward the verified email to the API on each call (a small wrapper around the fetches in `src/web/app/lib/api.ts`), and add the Cloudflare-Access `PrincipalCurrentUser` adapter to the API so `createdBy`/`modifiedBy` reflect the actual signed-in stakeholder instead of `StubCurrentUser`.

## Open Questions

- **Managed OAuth end-to-end.** Confirm against the live tenant that Claude / ChatGPT complete the Managed-OAuth flow against the `mcp` application (DCR + discovery + allow-list), and the exact `Cf-Access-Jwt-Assertion` claim set (`sub`, `email`) so `PrincipalCurrentUser` resolves a stable id.
- **CORS.** The API's `AllowAnyOrigin()` is moot while the API has no ingress, but revisit if anything other than `web` ever calls it.
