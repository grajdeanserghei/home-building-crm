# Remote MCP Server for AI-Driven Project Data Entry

- **Status:** Implemented <!-- Draft | In Review | Approved | Implemented | Deprecated -->
- **Author:** Serghei Grajdean
- **Date:** 2026-06-22
- **Related:** [`docs/architecture/domain-model.md`](../architecture/domain-model.md), [`docs/architecture/hexagonal-architecture.md`](../architecture/hexagonal-architecture.md), [`docs/project-overview.md`](../project-overview.md)

## Summary

Expose the application's project data to AI agents through a **remote MCP server** so that a connected agent (Claude or ChatGPT) can drive data entry conversationally and from documents. Two driving scenarios:

1. **Contractor & bid management (conversational CRM).** *"I've got a potential contractor for this work package — Ion Stratulat, phone 72186559, email …, recommended by Luci"* → the agent registers the contractor and opens a bid with a note. Later: *"I spoke with Stratulat, he'll send the BoQ by Monday next week"* → the agent logs a discussion note and moves the bid's status.
2. **Bill-of-Quantities ingestion (document).** The agent reads a PDF *deviz*, extracts its line items, and creates a BoQ via MCP — no human re-keying.

This spec covers the MCP server plus the prerequisite `Bid` and `BillOfQuantities` aggregates (neither is built yet; only `Contractor` exists).

## Motivation

The four stakeholders coordinate a duplex build with a handful of contractors per work package. The data is naturally captured *in conversation* ("so-and-so was recommended, I called them, they promised X by Y") and *in documents* (PDF *devize*). Both are tedious to enter through a form. An MCP server lets the agent the stakeholders already talk to write that data straight into the system, with every change attributed to the person whose token drove it.

The division of labour:

- For **CRM-style entry**, the agent turns free-text into structured tool calls — resolving relative dates ("Monday next week") to absolute ones, mapping a contractor lead onto register + open-bid + note.
- For **document ingestion**, the **local agent** reads the PDF (these models are multimodal) and emits structured line items; the **MCP server stays thin** — it validates, normalises units, enforces invariants, and persists. It never parses PDFs.

Goals:

- An agent can register/look up contractors, open and track bids (notes + status + expected dates), and create/revise/submit a BoQ.
- Writes are **attributable** (real `ICurrentUser`) and **auditable** (discussion notes are timestamped; BoQ ingestion is idempotent).
- The remote server is **authenticated** (OAuth 2.1) and restricted to the four stakeholders — it must not widen the currently-anonymous API to the public internet.

### Non-goals

- Server-side PDF parsing / OCR. The agent supplies structured data.
- A general write API for every aggregate — only the tool surface for contractor/bid management and BoQ ingestion (plus the read context to target the right project/work package).
- Replacing the REST API or the Next.js frontend.
- Building the Contract aggregate (only its id is referenced).
- The *human* sign-in flow for the web frontend (this spec wires real auth for the MCP adapter and the shared `ICurrentUser` seam; the frontend login is tracked separately).

## Design

### Overview

The MCP server is a **new driving adapter** in the hexagonal architecture — peer to the existing `ApiService` and the Next.js frontend — that calls the **Application** layer in-process.

```
Claude / ChatGPT desktop
        │  MCP over Streamable HTTP  (OAuth 2.1 bearer token)
        ▼
HomeProjectManagement.McpServer        ← new project (this spec)
   AddApplication() + AddInfrastructure()
   calls IContractorAppService, IBidAppService, IBillOfQuantitiesAppService, … in-process
        │
        ▼
Application  →  Domain (Contractor + new Bid + new BillOfQuantities)  →  Infrastructure (EF Core)  →  projectsdb
```

The MCP server references `Application` + `Infrastructure` and owns its own `AppDbContext`/`UnitOfWork` scope per request — identical composition to `ApiService`. No new application logic lives in the adapter; tools are thin wrappers over app-service calls, mirroring how `Endpoints/*.cs` wrap them today.

### Part 1 — Aggregates

Three aggregates back the tool surface. `Contractor` exists; `Bid` and `BillOfQuantities` must be built first, both following the conventions established by `Project`/`WorkPackage`/`Contractor`: strongly-typed `record struct` ids (auto-mapped to Guid columns), static `Create`/factory + intention-revealing mutators that raise domain events, `now` passed in, references to other aggregates **by id only**, internal entities reached only through the root.

#### 1a. `Contractor` — exists, lightly extended

Master data, reusable across work packages: `Name`, `FiscalCode?`, `RegistrationNumber?`, `Contact?` (`ContactInfo`), `Address?`, `Notes?`, with `Register` / `Rename` / `SetFiscalIdentifiers` / `ChangeContact` / `Relocate` / `Annotate`. No structural change required — `Notes` holds contractor-level provenance ("recommended by Luci" can live here, or as the opening bid note — see [where notes live](#where-do-notes-live)).

#### 1b. `Bid` — new (prerequisite)

A **bid is the engagement** of a contractor against a work package — the unit that carries the conversation, the status, the expected dates, and (once it arrives) the receipt of the submitted BoQ. This is where the CRM workflow lives.

> **Already implemented, extended here.** The `Bid` aggregate now exists (`src/HomeProjectManagement.Domain/Bids/`) with `BidStatus = InDiscussion | Quoted | Shortlisted | Selected | Rejected | Withdrawn` and `NoteType = Meeting | Call | Email | Note`. This spec **extends** it for the CRM/BoQ workflow with two new statuses (`BoqExpected`, `BoqReceived`), an `ExpectedBoqDate` field, and the matching mutators/events below. `BoqReceived` **supersedes the former `Quoted`** — a received priced BoQ *is* the quote — so `Quoted` is renamed. The canonical `BidStatus` enum in [`domain-model.md`](../architecture/domain-model.md) is updated to match.

```
Bid : AggregateRoot<BidId>
  ├─ WorkPackageId            // by id
  ├─ ContractorId             // by id
  ├─ Status : BidStatus       // InDiscussion | BoqExpected | BoqReceived | Shortlisted | Selected | Rejected | Withdrawn
  ├─ ExpectedBoqDate?         // e.g. "by Monday next week" → resolved absolute date (set with BoqExpected)
  ├─ FirstContactedOn?        // when discussions began (existing)
  ├─ Summary?                 // short standing of the bid (existing)
  ├─ Notes : IReadOnlyList<DiscussionNote>     // internal entities, append-only log
  │     └─ DiscussionNote : Entity<DiscussionNoteId>
  │           ├─ Type : NoteType   // Meeting | Call | Email | Note
  │           ├─ AuthorId : UserId // which stakeholder logged it
  │           ├─ Content
  │           └─ OccurredOn        // when the discussion happened (may precede CreatedOn)
  └─ events: BidOpened, DiscussionNoteLogged, BidStatusChanged, BidBoqExpected, BidBoqReceived
```

- Factory `Bid.Open(workPackageId, contractorId, now, …)` raises `BidOpened`; a fresh bid starts `InDiscussion`. Cross-aggregate guards (work package + contractor must exist) live in the app service, exactly as `WorkPackageAppService.DefineAsync` checks the parent project today.
- `LogNote(type, occurredOn, authorId, content, now)` appends a `DiscussionNote` and raises `DiscussionNoteLogged`. Notes are the append-only discussion log / audit trail (a `RemoveNote` exists for correcting mistaken entries).
- `ChangeStatus(status, now)` raises `BidStatusChanged` and enforces legal transitions (`Selected` is reachable only via `Select`, coordinated with the competing bids). **New:** `ExpectBoqBy(date, now)` sets `ExpectedBoqDate` and moves to `BoqExpected` (raising `BidBoqExpected`); `LinkBoq(boqId, now)` moves to `BoqReceived` (raising `BidBoqReceived`). The BoQ↔Bid link itself is carried canonically by `BillOfQuantities.BidId` (a bid may hold several BoQ versions), so `LinkBoq` records *receipt* via the status transition rather than storing a single BoQ pointer on the bid.
- **Legal transitions:** `InDiscussion → {BoqExpected, BoqReceived, Shortlisted, Rejected, Withdrawn}`; `BoqExpected → {BoqReceived, InDiscussion, Rejected, Withdrawn}`; `BoqReceived → {Shortlisted, Rejected, Withdrawn}`; `Shortlisted → {Rejected, Withdrawn}`; any non-terminal → `Selected` via `Select`. `Withdrawn` is terminal; `Selected` is set only through the award flow.

#### 1c. `BillOfQuantities` — new (prerequisite)

The structured *deviz*. Root with `Section`/`LineItem` internal entities; by-id references; `Money`/`ExchangeRate` VOs.

```
BillOfQuantities : AggregateRoot<BillOfQuantitiesId>
  ├─ WorkPackageId, ContractorId?     // by id
  ├─ Title, Status (BoqStatus: Draft | Submitted | Accepted | Superseded)
  ├─ PricingCurrency : Currency, ExchangeRate?
  ├─ SourceDocument? : DocumentReference, SourceContentHash?   // provenance + idempotency
  └─ Sections : Section[]  →  LineItems : LineItem[]
        LineItem: Description, UnitOfMeasureId, Quantity, UnitPrice : Money, LineTotal : Money (derived)
```

- Factory `Create`; mutators `AddSection`, `AddLineItems`, `ReviseLineItem`, `Submit`, `Supersede`.
- Every line shares `PricingCurrency` (`Money` rejects cross-currency arithmetic); `LineTotal = Quantity × UnitPrice` is computed in the aggregate.
- `UnitOfMeasureId` must reference an **active** `UnitOfMeasure`; free-text unit tokens ("mc", "m³", "buc") are normalised in the app service via `UnitOfMeasure.Recognizes(token)` before the line reaches the aggregate.

#### How the three relate

```
WorkPackage ──< Bid >── Contractor          (a contractor bids on a work package)
                 │
                 └─ BillOfQuantitiesId ──▶ BillOfQuantities ── WorkPackage
```

A `Bid` links a `Contractor` to a `WorkPackage`; once the *deviz* arrives, the `BillOfQuantities` references its owning `Bid` by id (`BillOfQuantities.BidId`, per `domain-model.md`) and the bid moves to `BoqReceived`. The link is one-way by id (BoQ → Bid), honouring the by-id rule. When a `BillOfQuantities` is accepted, the **app service** synchronously moves the owning `Bid` to `Selected` and awards the work package — the same selection/award flow `domain-model.md` already describes as application-service-coordinated. (Resolved: synchronous in the app service rather than via an `IDomainEventHandler`; the handler seam stays available for genuinely eventual concerns later.)

#### Infrastructure & Application (both new aggregates)

- `DbSet<Bid>` + `DbSet<BillOfQuantities>` on `AppDbContext`; `BidConfiguration` / `BillOfQuantitiesConfiguration` mapping notes/sections/line-items as owned collections (`OwnsMany`) or child tables with the root as required principal; `Money` flattened to amount+currency columns (as `Contractor`'s owned VOs are today); repositories registered in `Infrastructure/DependencyInjection.cs`; EF migrations.
- `IBidAppService` and `IBillOfQuantitiesAppService` (+ DTOs/commands) registered in `Application/DependencyInjection.cs`.
- **Finish wiring `UnitOfMeasure`** (register `IUnitOfMeasureAppService`, add its migration + a read endpoint) — BoQ line items normalise onto it and the agent needs to query the vocabulary. Existing gap flagged during design.

### Part 2 — The remote MCP server

**Project:** `HomeProjectManagement.McpServer`, an ASP.NET Core host using the official C# SDK `ModelContextProtocol.AspNetCore` (add a `<PackageVersion>` to `Directory.Packages.props`; reference version-less per CPM). Shared MSBuild props come from `Directory.Build.props`.

**Transport:** **Streamable HTTP** — the transport remote MCP clients (Claude desktop, ChatGPT) use to reach a networked server.

**Composition root** (`Program.cs`) mirrors `ApiService`:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("projectsdb");
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddMcpServer()
    .WithHttpTransport()          // Streamable HTTP
    .WithToolsFromAssembly();     // discovers [McpServerTool] methods

builder.Services.AddAuthentication(/* JWT bearer + MCP resource metadata — Part 3 */);
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp().RequireAuthorization();
app.Run();
```

> The exact `ModelContextProtocol.AspNetCore` API surface must be confirmed against the installed package version at implementation time; the shape above is the design intent.

**Tool implementation pattern** — thin methods that inject the app-service port and delegate, no domain/EF logic (same discipline as `WorkPackageEndpoints.cs`):

```csharp
[McpServerToolType]
public sealed class BidTools
{
    [McpServerTool, Description("Add a dated discussion note to a bid (a call, meeting, email, or commitment).")]
    public static async Task<BidDto> AddBidNote(
        IBidAppService service, AddBidNoteCommand command, CancellationToken ct)
        => await service.AddNoteAsync(command, ct);
}
```

### MCP tool surface

Tool I/O reuses the existing Application DTOs/commands, so the MCP schema and the REST contract stay in lockstep. Read tools return the same DTOs the REST API exposes.

**Context (read)** — let the agent target the right place:

| Tool | Purpose |
|---|---|
| `list_projects` | Find the project. |
| `list_work_packages` | Work packages under a project. |
| `list_contractors` | Find/disambiguate a contractor (avoid duplicates before registering). |
| `list_units_of_measure` | Active unit vocabulary (code + aliases) for BoQ line-item mapping. |

**Contractors & bids (write/read)** — the conversational CRM surface:

| Tool | Purpose |
|---|---|
| `register_contractor` | Create a contractor from structured fields (name, phone, email, fiscal code, …). Returns `contractorId`. |
| `update_contractor` | Edit contact / fiscal identifiers / address. |
| `annotate_contractor` | Set contractor-level master notes (e.g. provenance). |
| `open_bid` | Open a bid linking a contractor to a work package; optional opening note. Returns `bidId`. |
| `add_bid_note` | Append a dated discussion note (`type`, `text`, `occurredOn`). |
| `set_bid_status` | Move bid status; optionally set `expectedBoqDate`. |
| `list_bids` | Bids by work package or contractor. |
| `get_bid` | Read a bid with its full note history and status. |

**Bills of quantities (write/read)** — the document-ingestion surface:

| Tool | Purpose |
|---|---|
| `create_boq` | Create a draft BoQ under a work package (title, pricing currency, optional pinned exchange rate, optional contractor, source-document ref + content hash). Returns `boqId`. |
| `add_boq_sections` | Add sections (name, code, sequence). |
| `add_boq_line_items` | **Bulk** add line items to a section: `{description, unit, quantity, unitPrice}[]`. Server normalises `unit` → `UnitOfMeasureId`, computes totals, returns per-line results flagging unresolved units. |
| `revise_boq_line_item` | Correct a single line on a draft BoQ. |
| `remove_boq_line_item` | Remove a single line from a draft BoQ. |
| `add_boq_subsections` | Add subsections (fixed second level) to a section: `{name, sequence?, description?}[]`. |
| `add_boq_subsection_line_items` | **Bulk** add line items to a subsection — the subsection counterpart of `add_boq_line_items`, with the same unit normalisation and unresolved-line flagging. |
| `revise_boq_subsection_line_item` | Correct a single line inside a subsection of a draft BoQ. |
| `remove_boq_subsection_line_item` | Remove a single line from a subsection of a draft BoQ. |
| `list_boqs` | List the BoQ versions submitted within a bid. |
| `get_boq` | Read back a BoQ (sections, subsections, line items, totals) to verify. |
| `submit_boq` | Transition draft → `Submitted` (locks line edits); links the owning bid (`LinkBoq`). |

Design notes:

- **`add_boq_line_items` is bulk and partial-failure-aware.** A 200-line *deviz* is one or a few calls. Lines whose unit can't be matched to an active `UnitOfMeasure` come back flagged with the offending token; resolvable lines still persist, so a single bad unit doesn't fail the batch.
- **Relative dates are the agent's job.** "Monday next week" is resolved to an absolute ISO date by the agent (it knows today's date) before calling `set_bid_status` / `add_bid_note`. Commands accept absolute dates only.
- **Tool descriptions are prescriptive about *when* to call** (e.g. "call `list_contractors` before `register_contractor` to avoid duplicates"), which materially improves tool selection on current models.

### Worked flows

**A — Capture a contractor lead** (*"potential contractor for this work package: Ion Stratulat, phone 72186559, email …, recommended by Luci"*):

```
1. list_work_packages              → resolve the work package id.
2. list_contractors                → check Stratulat isn't already registered.
3. register_contractor(name="Ion Stratulat", phone="72186559", email="…")  → contractorId
4. open_bid(workPackageId, contractorId,
            note={type: General, text: "Potential contractor, recommended by Luci"})  → bidId
   (status starts InDiscussion)
```

**B — Log a discussion and update status** (*"I spoke with Stratulat, he'll send the BoQ by Monday next week"*):

```
1. list_bids(contractorId or workPackageId)   → resolve bidId (or the agent carries it from flow A).
2. add_bid_note(bidId, type: Call,
                text: "Discussed by phone; promised to send the BoQ.",
                occurredOn: 2026-06-22)
3. set_bid_status(bidId, status: BoqExpected, expectedBoqDate: 2026-06-29)   // "Monday next week" resolved
   (ExpectBoqBy sets ExpectedBoqDate and moves the bid to BoqExpected)
```

**C — Ingest the PDF BoQ when it arrives:**

```
1. list_units_of_measure                        → token→unit map.
2. (agent reads the PDF)                         → sections + line items as JSON.
3. create_boq(workPackageId, contractorId, currency, sourceContentHash, …)  → boqId
4. add_boq_sections; add_boq_line_items (bulk)   → server normalises units, computes totals.
5. get_boq → verify; fix unresolved units; submit_boq → links the bid (BoqReceived).
```

**Idempotency (flow C).** `create_boq` carries a `sourceContentHash` (SHA-256 of the PDF, computed by the agent). The app service returns the existing BoQ for a repeat hash under the same work package, so re-running an interrupted ingestion doesn't duplicate. The hash is stored for audit.

**Auditing (all flows).** Every write runs through the existing `UnitOfWork`, which stamps `CreatedBy`/`ModifiedBy` from `ICurrentUser`. With real auth (Part 3), those record *which stakeholder's* token drove the change. Bid `DiscussionNote`s additionally carry their own `OccurredOn`, giving a timestamped engagement history independent of row audit fields.

### Part 3 — Authentication & authorization

A *remote* MCP server sits in front of an API that currently has **no auth** (`StubCurrentUser` returns a hard-coded `UserId`). Shipping it open would expose write access to the build data on the internet. This spec wires the OAuth 2.1 authorization the project mandate already requires.

**IdP (resolved): Microsoft Entra External ID with Google federation.** The four stakeholders use Gmail accounts, so the tenant is configured as an **Entra External ID** (customer-facing) tenant with **Google** as a federated social identity provider — each stakeholder signs in with their existing Gmail account, and Entra mints the OIDC tokens this server validates. Entra is the authorization server; the stakeholder allow-list is enforced both by the tenant's limited membership and by an authorization-policy check on the resource server.

**Model:** the MCP server is an OAuth 2.1 **resource server**. It does not issue tokens; it validates bearer access tokens minted by Entra External ID for the four stakeholders. Per the MCP authorization spec it publishes **protected-resource metadata** (RFC 9728) at `/.well-known/oauth-protected-resource`, so a client can discover the authorization server and run the standard OAuth flow (Authorization Code + PKCE `S256`). Claude and ChatGPT desktop clients implement this discovery + flow natively when adding a remote MCP server URL.

> **Client registration caveat (Entra).** Entra External ID has **limited Dynamic Client Registration** support, so the desktop clients cannot self-register against it the way they can with Auth0/Keycloak. Plan for a **pre-registered public client** (an Entra app registration for the MCP clients, surfaced via Client ID Metadata Documents / a shared public `client_id` with PKCE) rather than relying on DCR. Confirm against the installed Entra External ID capabilities at implementation time.

```
client ──(1) GET /.well-known/oauth-protected-resource──▶ McpServer (advertises IdP)
client ──(2) OAuth 2.1 + PKCE authorization code flow ──▶ IdP  (stakeholder signs in)
client ──(3) MCP calls with Authorization: Bearer <JWT> ─▶ McpServer
McpServer ── validate JWT (issuer, audience, signature, scope)
          ── map sub/email → UserId via ICurrentUser → audit stamps
```

- `AddAuthentication().AddJwtBearer(...)` validates issuer, audience (this server's resource id), signature (Entra JWKS), and expiry; the MCP endpoint is `.RequireAuthorization()`.
- **Resource-bound tokens (RFC 8707).** ChatGPT (and Claude) append `resource=<this server's URL>` to the authorization and token requests and expect it echoed into the token's `aud` claim; the JWT-bearer **audience** validation above is exactly what enforces this — configure the audience to the server's resource id.
- A **real `ICurrentUser` adapter** replaces `StubCurrentUser` for this host, mapping the token subject/email to the stakeholder's `UserId`. Because `ICurrentUser` is the existing seam, this is an Infrastructure adapter swap — Application and Domain are untouched.
- **Restricted to the four stakeholders** — no open sign-up — via the Entra External ID tenant's limited membership plus an authorization-policy allow-list check (e.g. on verified email).
- Scope: a single `project:write` (and implicit read) scope suffices for four trusted users; finer scopes are a future refinement.

Once proven here, this adapter is the same mechanism the REST `ApiService` and the web frontend will adopt — this spec delivers the project's first real authentication.

### Part 4 — Aspire orchestration

```csharp
var mcp = builder.AddProject<Projects.HomeProjectManagement_McpServer>("mcpserver")
    .WithReference(projectsDb)
    .WaitFor(projectsDb)
    .WithExternalHttpEndpoints();        // remote clients connect from outside the Aspire network
```

`projectsdb` is the contractual resource name already consumed via `AddNpgsqlDbContext<AppDbContext>("projectsdb")`. `.WithExternalHttpEndpoints()` is required because this endpoint must be reachable by remote agent clients (unlike the internal API). IdP configuration (authority, audience, JWKS) comes from configuration/user-secrets, not committed.

### Implementation order

1. `Bid` aggregate + repository + EF config + migration (Domain → Infrastructure), and `IBidAppService` + DTOs — unblocks the CRM scenario, which needs no new units/BoQ work.
2. Contractor/bid MCP tools + the MCP server project (composition root, Aspire wiring), initially network-restricted — delivers flows A & B end-to-end.
3. `BillOfQuantities` aggregate + repository + EF config + migration; finish `UnitOfMeasure` wiring; `IBillOfQuantitiesAppService` (unit normalisation, idempotency).
4. BoQ MCP tools — delivers flow C.
5. OAuth 2.1 resource-server auth + real `ICurrentUser` adapter; flip on `.WithExternalHttpEndpoints()`.

## Design Decisions

Resolved with the maintainer:

- **PDF parsing — local agent extracts; server validates/persists.** Keeps the server thin and free of OCR.
- **Integration — separate project, in-process app services.** `HomeProjectManagement.McpServer` references `Application` + `Infrastructure` and calls app-service ports directly.
- **Auth — OAuth 2.1 / OIDC (the MCP standard).** Resource-server token validation against an external IdP, restricted to the four stakeholders; doubles as the project's first real authentication and wires `ICurrentUser`.
- **The bid is the engagement.** Discussion notes, status, and expected dates live on `Bid`, not on `Contractor` (master data) or `WorkPackage`; the BoQ links back to its bid by id.
- **`BidStatus` values & transitions (resolved — extend the built model).** The implemented `Bid` is extended with `BoqExpected` and `BoqReceived`; `BoqReceived` supersedes `Quoted`. Final enum: `InDiscussion | BoqExpected | BoqReceived | Shortlisted | Selected | Rejected | Withdrawn`, with the legal transitions listed in [Part 1b](#1b-bid--new-prerequisite). `domain-model.md` is updated to match.
- **`NoteType` values (resolved).** Use the implemented `Meeting | Call | Email | Note` (the draft's `Commitment`/`General` map onto `Note`). No `Commitment` type is added.
- **Where "recommended by Luci"-style notes live (resolved).** Both are supported; the recommendation is the opening **bid `DiscussionNote`** when the lead surfaces "for this work package", and contractor-level `Notes` for reusable, work-package-independent provenance.
- **Relative-date resolution (resolved — strictly absolute).** The agent resolves "Monday next week" → absolute ISO date before calling; commands accept absolute dates only. This keeps the "no clock in the domain" rule intact — no relative-string fallback.
- **Bid ↔ BoQ coordination (resolved — synchronous).** When a BoQ is accepted, the **app service** synchronously moves the owning bid to `Selected` and awards the work package. The `IDomainEventHandler` seam is left for later eventual-consistency needs.
- **IdP (resolved — Entra External ID + Google federation).** Stakeholders sign in with their Gmail accounts via Google federation into an Entra External ID tenant. Caveat: limited DCR → use a pre-registered public client (CIMD / shared `client_id` + PKCE) for the desktop clients.
- **ChatGPT connector parity (resolved — full parity).** ChatGPT's remote-MCP connector implements the same OAuth 2.1 flow as Claude: RFC 9728 protected-resource-metadata discovery, Authorization Code + PKCE (`S256`), and DCR/CIMD. No manual token step. It additionally sends `resource=` (RFC 8707), enforced by the server's JWT audience validation.
- **Unit handling (resolved — block & flag).** When a line-item unit token has no active `UnitOfMeasure`, resolvable lines still persist and unresolved lines come back flagged with the offending token for an admin to add; the agent does not auto-create units (protects the controlled vocabulary, per `domain-model.md`).

## Open Questions

_All design questions are resolved (see Design Decisions). Remaining items are downstream of those decisions and tracked at implementation time:_

- **Entra External ID specifics.** Confirm, against the actual tenant, the public-client registration approach (CIMD vs shared `client_id`) the Claude/ChatGPT desktop clients accept, and the exact protected-resource-metadata `authorization_servers` entry for an External ID tenant.

## Implementation notes

Delivered against the existing (further-along-than-this-spec-assumed) codebase, where `Bid`,
`BillOfQuantities`, `Contract`, and `UnitOfMeasure` were already built and wired. A few small,
deliberate deviations from the design above, recorded for the record:

- **`ModelContextProtocol.AspNetCore` 1.4.0** (current stable) is the SDK version used. `Program.cs`
  matches the design: `AddMcpServer().WithHttpTransport().WithToolsFromAssembly(...)` + `MapMcp()`.
  Tool I/O is configured to serialize enums as their string names, keeping the MCP schema in lockstep
  with the REST contract and the frontend's TypeScript enums.
- **`create_boq` takes a `bidId`, not `workPackageId`+`contractorId`.** A BoQ belongs to a bid (per
  `domain-model.md`); the tool description directs the agent to `open_bid` first. This honours the
  by-id rule rather than re-deriving the bid inside the tool.
- **`BoqStatus` keeps the implemented/`domain-model.md` set** (`Draft | Submitted | Accepted |
  Rejected | Withdrawn`) — the draft's `Superseded` maps onto `Withdrawn` (the existing semantics of
  "pulled back or superseded by a later version"). No enum churn was introduced.
- **`UnitOfMeasure` was already fully wired** (app service, endpoint, migration, seed), so the
  "finish wiring" item was a no-op beyond consuming it for `list_units_of_measure` and unit
  normalisation.
- **Auth is config-gated (`McpAuth:Enabled`).** It ships off so the host runs network-restricted with
  the existing `StubCurrentUser` (matching implementation-order steps 1–4); setting it on activates
  the JWT-bearer resource server, the RFC 9728 protected-resource metadata, the stakeholder allow-list
  policy, and the real principal-based `ICurrentUser` adapter (step 5). Entra authority/audience/
  allow-list come from user-secrets, never committed. `.WithExternalHttpEndpoints()` is wired in the
  AppHost.
