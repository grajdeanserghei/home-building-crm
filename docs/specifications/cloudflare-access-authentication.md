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
                       ├── Access app "web"  ──> web container :3000   (browser login)
                       └── Access app "mcp"  ──> mcp container :8080   (Managed OAuth)
                                                    │
                          (private, no ingress) ────┼──> api container :8080
                                                    └──> postgres
```

- Both Access applications forward a signed **`Cf-Access-Jwt-Assertion`** (also a `CF_Authorization` cookie) carrying the verified email to their origin.
- Token validation contract: **issuer** (`iss`) = the team domain `https://<team>.cloudflareaccess.com`; **signing keys** (JWKS) at `https://<team>.cloudflareaccess.com/cdn-cgi/access/certs` (raw JWKS; Cloudflare keeps the current + previous key live); **audience** (`aud`) = each application's unique **AUD tag**.
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

| Key | Meaning |
|---|---|
| `Enabled` | Turns on origin-side validation + the allow-list re-check. `false` in local dev. |
| `TeamDomain` | `https://<team>.cloudflareaccess.com` — the issuer and JWKS base. |
| `Audience` | The Access application's AUD tag. |
| `AllowedEmails__0,1,…` | Defense-in-depth allow-list (the edge Access policy is the primary gate). |

Container env keys use the `CloudflareAccess__*` form — see [`container-images.md`](../guides/container-images.md).

### Infrastructure (executed in `home-lab-infra` / Cloudflare dashboard)

1. Create a Cloudflare Tunnel + `cloudflared` ingress mapping the public hostnames to the `web` (`:3000`) and `mcp` (`:8080`) k3s services. No ingress for the API or Postgres.
2. In Cloudflare Zero Trust: add **Google** as a login method; create two **self-hosted Access applications** (`web`, `mcp`); attach an **Allow** policy whose selector is the four stakeholders' `Emails`. Enable **Managed OAuth** on `mcp` (set allowed redirect URIs; allow localhost/loopback clients for desktop MCP clients).
3. Note each app's **AUD tag** + the team domain — these become the backend config above.
4. Update the deploy runbook in `home-lab-infra`.

### Shared-code note

The auth building blocks currently live in the `HomeProjectManagement.McpServer` project, because only the MCP server is authenticated in this pass. When the API later adopts the same validation (the follow-up below), lift `CloudflareAccessOptions`, `CloudflareAccessAuthExtensions`, and `PrincipalCurrentUser` into `Infrastructure` (or a small `*.Auth` library) rather than duplicating them.

## Follow-up

**Real per-user audit identity for the web UI.** Have `web` read `Cf-Access-Jwt-Assertion` server-side (via `next/headers`), forward the verified email to the API on each call (a small wrapper around the fetches in `src/web/app/lib/api.ts`), and add the Cloudflare-Access `PrincipalCurrentUser` adapter to the API so `createdBy`/`modifiedBy` reflect the actual signed-in stakeholder instead of `StubCurrentUser`.

## Open Questions

- **Managed OAuth end-to-end.** Confirm against the live tenant that Claude / ChatGPT complete the Managed-OAuth flow against the `mcp` application (DCR + discovery + allow-list), and the exact `Cf-Access-Jwt-Assertion` claim set (`sub`, `email`) so `PrincipalCurrentUser` resolves a stable id.
- **CORS.** The API's `AllowAnyOrigin()` is moot while the API has no ingress, but revisit if anything other than `web` ever calls it.
