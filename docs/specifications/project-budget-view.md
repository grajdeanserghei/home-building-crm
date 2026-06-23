# Project Budget View

- **Status:** Implemented <!-- Draft | In Review | Approved | Implemented | Deprecated -->
- **Author:** Serghei Grajdean
- **Date:** 2026-06-23
- **Related:** [`docs/architecture/domain-model.md`](../architecture/domain-model.md), [`docs/architecture/hexagonal-architecture.md`](../architecture/hexagonal-architecture.md), [`docs/project-overview.md`](../project-overview.md)

## Summary

A new read-only page, `/projects/{id}/budget`, that shows — for every work package in a project — what it is going to cost: the **committed** figure (`Contract.Value`) once a work package is awarded, or the **candidate** figure (the range of received bill-of-quantities totals) while it is still out to bid. The page rolls those up into a **projected cost of the whole build**, kept **per currency** because the domain refuses to sum across currencies.

## Motivation

The four stakeholders need to answer one question repeatedly while the duplex is being built: *"where are we on cost?"* Today there is no way to see it. The money exists in the system but is scattered:

- A work package that has been **awarded** has a `Contract` whose `Value` is the agreed price.
- A work package still **out to bid** has one or more `Bid`s, each of which may carry one or more `BillOfQuantities` versions, and the BoQ's derived `Total` is the candidate price.
- A work package with bids but **no priced BoQ yet** has no number at all.

No screen joins these. The project detail page lists work packages with status and dates but no money; the work-package detail page shows bids and a contract link but never the contract value or any BoQ total. To get a cost picture a stakeholder would have to open every work package, every bid, and every BoQ by hand and add them up — across two currencies, which the domain deliberately won't do for them.

This spec closes that gap with a single composed read model and one page.

Goals:

- One screen lists every work package with its current cost figure, picking **contract value when awarded** and **bid BoQ range otherwise** automatically.
- A project-level **projected total** = committed contracts + an estimated band for everything still open, shown **per currency** with no silent cross-currency conversion.
- No change to the domain model, the database schema, or any mutation flow — this is purely a read/query feature.

### Non-goals

- **No owner-side budget/target field.** There is no "planned budget" stored on `Project`, `WorkPackage`, or `ScopeItem`, and this spec does not add one. The page reports projected *cost* (what bids/contracts say), not variance against a target. Introducing a target to compare against is a possible future spec.
- **Native per-currency totals stay primary.** `Money` rejects cross-currency arithmetic by design; the page shows RON and EUR subtotals side by side as the authoritative figures. A single **approximate EUR-equivalent** grand total is shown *in addition* (computed from one app-wide display rate, clearly labelled), not in place of them.
- **No new mutations.** No editing of contract values or bids from this page; it is read-only. Awarding still happens through the existing contract-award flow.
- **No per-scope-item rollup.** BoQ sections do not reference `WorkPackage.ScopeItem`s, so cost cannot currently be attributed to individual scope items. Out of scope here.
- **No schema/migration change.** The feature composes existing aggregates through existing repositories; it introduces no `DbSet`, configuration, or migration.

## Requirements

- [ ] `GET /api/projects/{id}/budget` returns a `ProjectBudgetDto`: one line per work package plus per-currency totals. `404` if the project does not exist.
- [ ] Each work-package line classifies itself as `Contract`, `Bids`, `Pending`, or `None` and carries the matching figure(s).
- [ ] For an awarded work package the line shows `Contract.Value`.
- [ ] For a work package out to bid the line shows the **low–high range** of received BoQ totals plus the **count** of priced bids, grouped so currencies are never mixed in one range.
- [ ] Project totals are computed **per currency**: committed (sum of contracts), estimated band (sum of low / sum of high of open work packages), and a projected band = committed + estimated.
- [ ] All figures are **VAT-inclusive (gross)** — a budget is what will actually be paid. Bid ranges use the BoQ `TotalWithVat`; the contract's net agreed value is grossed up by the accepted BoQ's effective VAT ratio. Money column headers state the VAT basis explicitly (*"(cu TVA)"*).
- [ ] An **approximate EUR-equivalent** projected total is shown alongside the per-currency totals, computed from a single app-wide display rate and labelled with the rate used.
- [ ] A new Next.js page at `/projects/[id]/budget`, linked from the project detail page, renders the above with Romanian labels and `formatMoney`.

### Non-goals

(See the section above.)

## Design

### Where the money actually is

A reminder of the data shape, because the whole design follows from it:

```
WorkPackage ──< Bid >── Contractor
   │             │
   │             └─ (by id)  BillOfQuantities.BidId ──▶ BillOfQuantities { Total, TotalWithVat, PricingCurrency }
   │
   └─ AwardedContractId ──▶ Contract { Value : Money, Status }
```

- `WorkPackage` itself holds **no money** — only `ProjectId`, `Status`, sequence, dates, scope items, and (once awarded) `AwardedContractId`.
- `Contract.Value` is a `Money` (defaults to the accepted BoQ total, may be negotiated). One contract per awarded work package, fetched today via `GET /api/work-packages/{id}/contract`.
- `BillOfQuantities.Total` / `TotalWithVat` are **derived** `Money` values (rolled up from line items in the aggregate). A BoQ belongs to a `Bid` by id; a bid may have several BoQ versions; each BoQ pins its own `PricingCurrency` (RON or EUR).

So a per-work-package cost figure must be **assembled** from these three aggregates. That assembly is the feature.

### Which figure wins, per work package

Each work-package line is classified by a `kind`, decided in this order:

| Order | Condition | `kind` | Figure shown |
|---|---|---|---|
| 1 | `AwardedContractId` is set (status `Awarded`/beyond, contract exists) | `Contract` | **committed** = `Contract.Value` |
| 2 | ≥1 bid has a received/priced BoQ | `Bids` | **candidate** = low–high range of BoQ totals + count, per currency |
| 3 | Has bids but none priced yet | `Pending` | — ("price awaited") |
| 4 | No bids | `None` | — ("no bids") |

Notes on the rules:

- **Which BoQ counts as a bid's price.** A bid can hold several BoQ versions. The candidate figure uses each bid's **current/latest non-superseded** BoQ `Total` (the same one the bid's `BoqReceived`/`Selected` lifecycle refers to). One price per bid; the range is taken across bids, not across versions.
- **Currency grouping.** Bids on the same work package could in principle quote in different currencies. The range is computed **within a currency**; if a work package has both RON and EUR bids, the line shows a sub-range per currency rather than a meaningless mixed min/max. In practice almost all bids are RON; the grouping just keeps the rule honest.
- **Awarded wins outright.** Once awarded, the candidate bids are no longer shown as the figure (the decision is made); the committed contract value is the number. The losing bids remain visible on the work-package detail page, not here.

### Project totals — projected cost, per currency

Because `Money.Add` throws across currencies, totals are a **block per currency** (a RON block, and an EUR block only if any EUR figure exists). Within each currency the projection is a **band**, since open work packages are ranges:

```
RON
  Committed (contracts):        420 000
  Estimated (open, low–high):   175 000 – 210 000
  ──────────────────────────────────────────────
  Projected total:              595 000 – 630 000        (3 work packages still unpriced)

EUR
  Committed:                     12 000
  Estimated:                          —
  ──────────────────────────────────────────────
  Projected total:               12 000
```

- **Committed** = Σ `Contract.Value` over awarded work packages in that currency.
- **Estimated** = (Σ range-low, Σ range-high) over `Bids`-kind work packages in that currency.
- **Projected total** = committed + estimated (a band).
- **Unpriced footnote.** `Pending`/`None` work packages contribute nothing to the band; the page footnotes how many there are so the projected total is not misread as complete.

All figures are **VAT-inclusive (gross)** — a budget is what will actually be paid. Bid ranges use the BoQ `TotalWithVat`; the contract figure is grossed up (see below). The money column headers say so explicitly (*"(cu TVA)"*) so the VAT basis is never ambiguous.

**Grossing up the contract figure.** `Contract.Value` is stored **net** and carries no VAT rate of its own, so the budget derives its gross by applying the accepted BoQ's *effective* VAT ratio — `TotalWithVat / Total` — to the agreed value. This honours the BoQ's actual per-line VAT mix (not a flat 21%) and any negotiated value, and falls back to the agreed value when the accepted BoQ is missing or has a zero total. The query therefore loads the accepted BoQ (`contract.AcceptedBoqId`) for awarded work packages.

#### EUR-equivalent total

Each work-package line also carries an **EUR (gross) column** — its figure converted to EUR (a single value for an awarded line, the converted candidate band for a bid line) — so individual packages can be compared in one currency. Below the native per-currency totals, the page shows one **approximate EUR-equivalent** figure (committed / estimated band / projected band), so the whole build can be read at a glance in a single currency without violating the no-cross-currency-`Money` rule. It is computed in the query, not the client: each per-currency total is converted to EUR through the existing `IExchangeRateProvider` port and summed. Conversion uses a **single app-wide display rate** (`ManualExchangeRateProvider`, default `1 EUR = 5.07 RON`, overridable via the `EXCHANGE_RATE_RON_PER_EUR` environment variable), *not* the per-BoQ pinned rates — so it is explicitly labelled approximate and the rate is shown. The pinned per-BoQ `ExchangeRate` remains the source of truth for a specific quote. The row is only rendered when it adds information (more than one currency, or the single currency is not already EUR).

### Backend — a read/query use case (hexagonal-preserving)

The rollup is a **reporting query**, not new domain behaviour, so it lives in the **Application** layer as a read service that is explicitly permitted to read across aggregates — the one place the "which figure wins / how it sums" rule is encoded, so the project page and any future page can't drift apart.

```
ApiService  GET /api/projects/{id}/budget
   └─▶ IProjectBudgetQuery.GetAsync(projectId, ct)        ← new, Application layer
          ├─ IWorkPackageRepository   (work packages of the project)
          ├─ IBidRepository           (bids per work package)
          ├─ IBillOfQuantitiesRepository (current BoQ totals per bid)
          └─ IContractRepository      (contract per awarded work package)
        composes → ProjectBudgetDto
```

It uses the existing repository ports (read-only); it does **not** add a `DbSet`, an `IEntityTypeConfiguration`, or a migration. If the per-work-package fan-out proves chatty, it can later be backed by a single read-optimised EF query/projection in Infrastructure behind the same port — but the first cut composes the existing repositories, keeping the change small and the schema untouched.

DTOs (new, in `Application/`, returned as-is by the endpoint):

```csharp
ProjectBudgetDto(
    Guid ProjectId,
    string ProjectName,
    IReadOnlyList<WorkPackageBudgetLineDto> Lines,
    IReadOnlyList<CurrencyTotalsDto> TotalsByCurrency,
    int UnpricedWorkPackageCount);

WorkPackageBudgetLineDto(
    Guid WorkPackageId,
    string Name,
    WorkPackageStatus Status,
    int Sequence,
    BudgetLineKind Kind,                 // Contract | Bids | Pending | None
    MoneyDto? Committed,                 // Contract.Value when Kind == Contract
    IReadOnlyList<CandidateRangeDto> Candidates);   // per-currency range when Kind == Bids

CandidateRangeDto(
    Currency Currency,
    MoneyDto Low, MoneyDto High,
    MoneyDto LowWithVat, MoneyDto HighWithVat,
    int BidCount);

CurrencyTotalsDto(
    Currency Currency,
    MoneyDto Committed,
    MoneyDto EstimatedLow, MoneyDto EstimatedHigh,
    MoneyDto ProjectedLow, MoneyDto ProjectedHigh);
```

`BudgetLineKind` is a string-serialized enum, consistent with the rest of the API and the frontend's TypeScript unions. The endpoint is a thin minimal-API handler in a new `ProjectBudgetEndpoints.cs` (or folded into `ProjectEndpoints.cs`) that calls the query port and returns the DTO — the host touches neither EF nor the domain, per the existing pattern.

### Frontend

- **Fetcher + types** in `app/lib/api.ts`: `getProjectBudget(projectId)` (`cache: "no-store"`, like its siblings) plus `ProjectBudget`, `WorkPackageBudgetLine`, `CandidateRange`, `CurrencyTotals`, and a `BudgetLineKind` union mirroring the C# enum.
- **Page** `app/projects/[id]/budget/page.tsx` — a server component:
  - a per-work-package table: `# | Work package | Status | Committed | Candidate bids`, where the last two columns render from `kind` (`Pending`/`None` show a muted label, not a number);
  - the per-currency totals block(s) described above, with the unpriced footnote;
  - money via `formatMoney`; labels via the `t()` / `ro.ts` i18n helper (a `budget.*` key group), consistent with the existing pages.
- **Link** from `app/projects/[id]/page.tsx` (project detail) to the budget page — a "Budget" / *"Buget"* action near the work-packages heading.

No new server actions (read-only feature).

### Implementation order

1. `IProjectBudgetQuery` + `ProjectBudgetDto` family in Application; implement the rollup composing the existing repositories; register in `Application/DependencyInjection.cs`.
2. `GET /api/projects/{id}/budget` endpoint returning the DTO.
3. `getProjectBudget` + types in `api.ts`; the `/projects/[id]/budget` page; the link + `ro.ts` labels.
4. (Optional, only if the fan-out is slow) replace the composed implementation with a single EF projection behind the same port.

## Design Decisions

Resolved with the maintainer:

- **Candidate display — range + count.** A work package out to bid shows the low–high band of its received BoQ totals and how many bids are priced, rather than only the lowest or a row per bid. Best for comparing offers at a glance.
- **Currency totals — subtotal per currency.** Totals are shown per currency (RON, EUR) with no conversion, faithful to `Money`'s no-cross-currency rule. There is deliberately no single grand total across currencies.
- **Page intent — projected cost of the whole build.** Totals emphasise committed contracts + a best-estimate band for everything still open, giving a forward-looking projection to track against, rather than only a static comparison board.
- **VAT-inclusive throughout (revised).** Every budget figure is gross — the amount that will actually be paid. Bid ranges use BoQ `TotalWithVat`; the net `Contract.Value` is grossed up by the accepted BoQ's effective VAT ratio. (This supersedes the original ex-VAT headline decision.)
- **Read query, not domain behaviour.** The rollup is an Application-layer reporting query reading across aggregates through existing repository ports — no domain changes, no schema/migration, no mutations.

## Open Questions

- **Cross-currency display total (resolved — approximate EUR equivalent).** An EUR-equivalent total is now shown alongside the per-currency subtotals, computed from a single app-wide display rate via `IExchangeRateProvider` and labelled approximate. A future refinement could prefer each figure's pinned per-BoQ rate (exact, but sparse — many BoQs have no rate) and fall back to the display rate only where absent.
- **"Current BoQ per bid" precise rule.** The candidate uses each bid's latest non-superseded BoQ. Confirm at implementation time how a bid with multiple live (non-superseded) versions should be treated — expected to be at most one in practice.
- **Performance.** First cut composes per-work-package repository calls. If a large project makes this chatty, fold into a single EF projection behind `IProjectBudgetQuery` (the port is designed to allow this without touching callers).

## Implementation notes

Delivered as designed — a read-only feature, no domain/schema/migration change. Files added/changed:

- **Application** — `Budgeting/ProjectBudgetDtos.cs` (DTO family + `BudgetLineKind` enum, reusing `Contracts.MoneyDto`), `Budgeting/IProjectBudgetQuery.cs`, `Budgeting/ProjectBudgetQuery.cs` (the cross-aggregate rollup, composing `IProjectRepository`/`IWorkPackageRepository`/`IBidRepository`/`IBillOfQuantitiesRepository`/`IContractRepository`); registered in `DependencyInjection.cs`.
- **ApiService** — `Endpoints/ProjectBudgetEndpoints.cs` (`GET /api/projects/{projectId}/budget`), mapped in `Program.cs`.
- **Frontend** — `ProjectBudget`/`WorkPackageBudgetLine`/`CandidateRange`/`CurrencyTotals`/`BudgetLineKind` types + `getProjectBudget` in `app/lib/api.ts`; the page `app/projects/[id]/budget/page.tsx`; a "Buget" link on the project detail page; `budget.*` labels in `app/lib/i18n/ro.ts`.

Two small concretisations of the design:

- **"Current priced BoQ per bid"** is the latest BoQ version that is not `Rejected`/`Withdrawn` **and** carries a positive net `Total` — an empty/zero draft does not make a bid count as priced (it shows as *Pending* instead).
- **An awarded contract counts toward `committed` regardless of contract status** (incl. `Terminated`); the awarded figure is still the work package's number. Treating a terminated contract as no-longer-committed is a possible future refinement.

Follow-up additions (EUR equivalent + VAT-in-header):

- **`ManualExchangeRateProvider` now supplies a cross EUR↔RON display rate** (it previously threw for cross-currency). Default `1 EUR = 5.07 RON`, overridable via `EXCHANGE_RATE_RON_PER_EUR`. No other code calls the port, so the behaviour change is self-contained.
- **`ProjectBudgetQuery` gained `IExchangeRateProvider` + `TimeProvider`** and emits `EurEquivalentDto { ronPerEur, totals }` on `ProjectBudgetDto`; the page renders it as a labelled row plus the rate, shown only when it adds information. Each `WorkPackageBudgetLineDto` additionally carries an `EurBandDto? EurEquivalent` (gross, same display rate, one conversion date shared with the total), rendered as an **EUR (cu TVA)** column in the work-packages table.
- **Budget is now VAT-inclusive throughout** (revised from the original ex-VAT headline). `CandidateRangeDto` carries only the gross `Low`/`High` (ranked by `TotalWithVat`); the committed figure is grossed up from the accepted BoQ's effective VAT ratio (`ContractGrossAsync`, which loads the BoQ by id). Totals and the EUR equivalent are gross by construction. Headers read *"(cu TVA)"*.
