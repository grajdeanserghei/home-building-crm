# Construction Valuation (Appraiser's *Fișă de calcul*)

- **Status:** Draft <!-- Draft | In Review | Approved | Implemented | Deprecated -->
- **Author:** Serghei Grajdean
- **Date:** 2026-07-15
- **Related:** [Domain Model](../architecture/domain-model.md), [Project Budget View](./project-budget-view.md), [Cost Scenarios](./cost-scenarios.md)

## Summary

Model the bank appraiser's construction-valuation document (*Fișă de calcul a valorii construcției*, segregated-cost method — *metoda costurilor segregate*) so its itemized estimate can be stored, its on-site completion assessments tracked over time, and each estimated line **compared against the owners' real BoQ costs**. Two aggregates: a stable, per-project **`ValuationCatalog`** (the itemized estimate + the mapping to real BoQ sections) and many dated **`ConstructionValuation`** snapshots (frozen completion assessments) hung off it.

## Motivation

For the mortgage, a licensed real-estate appraiser produces a valuation of the build: they list every *lucrare* the architectural project requires, price each from a **standard catalog** (MATRIX, fișa nr. 38 for this build), then visit the site and assess **what percentage of each is done**. The result is the source spreadsheet `Fisa-calcul-valoare-constructie-pag38.xlsx`.

This is fundamentally different from a contractor's `deviz`/BoQ:

| | Bill of Quantities (`deviz`) | Construction valuation (*fișă de calcul*) |
| --- | --- | --- |
| Author | The owners' contractors | The bank's appraiser |
| Prices from | Real market quotes | A standard catalog (fișă nr. 38) |
| Purpose | What the owners will actually pay | Mortgage valuation of completeness |
| The `%` column | — | On-site assessment of work done |

The owners want two things this enables:

1. **Compare estimated vs. real cost per work item** — is the appraiser's catalog value for "Beton armat în structură" close to what the owners are actually paying, per their BoQs? This requires mapping the appraiser's coarse items onto the owners' BoQ sections/subsections and rolling the BoQ totals up per item.
2. **Track completion over time** — each site visit is a dated snapshot; charting completed vs. remaining value across snapshots shows build progress and supports mortgage-tranche release.

## Requirements

- [ ] Store the appraiser's itemized estimate for a project: each item's name, unit (as printed), catalog source (`F.38`/`Deviz`/…), unit cost per built area, cost weight, and total value with/without VAT.
- [ ] Store the report header: valuation method, catalog reference, surfaces (built / gross-floor (SCD) / usable), own-regie adjustment, VAT rate, currency (RON).
- [ ] **One `ValuationCatalog` per project** (the baseline), editable in place.
- [ ] Map each catalog item to zero or more **BoQ `(BoqId, Section, Subsection?)`** targets, with **no BoQ section linked to more than one item** (no double-counting) and **a section mapped either as a whole or subsection-by-subsection, never both** (granularity exclusivity).
- [ ] Record **many dated `ConstructionValuation` snapshots** against that catalog, each capturing per item: the appraiser's completion %, and the completed/remaining values (with & without VAT).
- [ ] A snapshot is a **frozen historical fact**: its item values are computed **once at capture** and never recomputed on read; later catalog edits do not alter past snapshots.
- [ ] Idempotent import of a snapshot document (agent-parsed; the server never parses the file), keyed by a content hash — the same way BoQ ingestion works.
- [ ] Read models: (a) **estimate vs. real BoQ cost** per item (live), (b) **progress** per snapshot and across snapshots (frozen), each with project totals and a coverage measure for unmapped items.

### Non-goals

- **No self-assessment.** The completion % is only ever the appraiser's number; the owners do not record a competing estimate in this model.
- **No catalog versioning.** A revised report edits the catalog **in place**; historical fidelity is provided by the frozen snapshots, not by diffable catalog versions. (See Open Questions.)
- **No multiple parallel catalogs per project** (e.g. two banks' appraisers). One active catalog per project.
- **No cross-currency grand total.** RON and EUR are never summed (the `Money` value object forbids it); EUR equivalents are derived per snapshot from its pinned rate and shown separately.
- The server never parses `.xlsx`/PDF — parsing is the agent's job (as with BoQ ingestion).

## Design

The two clocks — a slow-moving estimate + mapping, and fast-moving dated assessments — are split into two aggregates.

### Aggregate `ValuationCatalog` (one per project)

The project's valuation baseline: the itemized estimate plus the BoQ mapping. Under `Domain/ValuationCatalogs/`. References `ProjectId` by id; one active catalog per project (enforced by the application service + a unique index on `projectId`). Editable in place.

Header: `method` (`SegregatedCost`), `catalogReference` (`"MATRIX, Fișa 38"`), surfaces `builtArea` / `grossFloorArea` (SCD) / `usableArea`, `ownRegieAdjustment` (0.20), `vatRate` (21%), `currency` (RON), `status` (`Draft`/`Active`).

Owns a flat list of **`ValuationCatalogItem`** (local entities):

- `sequence` + `printedNumber` — `printedNumber` preserves the appraiser's printed *Nr. Crt.* verbatim, quirks included (this report duplicates "25" and skips "27"); `sequence` is the stable ordering.
- `name`, `unit` (**raw text** — the printed column D, which mixes real units `mc`/`mp`/`ml`/`kg` with lump-sum markers `%`/`lei`/`RON`; not linked to the `UnitOfMeasure` vocabulary), `catalogSource` (`F.38`/`Deviz`/`F.26`/`F.24`), `costWeight`.
- `unitCostPerBuiltArea`, `totalCostWithoutVat` (col G), `totalCostWithVat` (**stored**, kept in sync — see VAT below).
- `isActive` — retired via `Deactivate` rather than hard-deleted, because snapshots reference it by id.
- owns **`ValuationItemLink`** value objects `{ boqId, sectionId, subsectionId? }` — the BoQ mapping.

**VAT** lives on the catalog. `ChangeVatRate(rate)` loops the items and recomputes each `totalCostWithVat` — a **write-time** recompute on current state. It does **not** touch existing snapshots.

**Mapping invariants** (both enforced by the root — only it sees every item's links):

- **No double-counting** — each `(boqId, sectionId, subsectionId)` triple is linked to **at most one** item.
- **Granularity exclusivity** — for one `(boqId, sectionId)`, a whole-section link (`subsectionId == null`) and its subsection links are **mutually exclusive**. A section is therefore mapped *either* as a whole — implicitly covering every subsection and direct line — *or* subsection-by-subsection (each subsection to any item), never both. Switching granularity requires unlinking first.

Both are checkable from the link tuples alone because a **subsection link stores that subsection's actual parent `sectionId`**. Populating that real parent section is the application service's job when it validates the link against the BoQ (the same loose-reference pattern as `Section → ScopeItem`) — the domain then needs no BoQ lookup.

### Aggregate `ConstructionValuation` (a dated snapshot; many per catalog)

A single site-visit assessment. Under `Domain/ConstructionValuations/`. References `valuationCatalogId` by id. A **frozen historical fact** — reads never recompute.

Header: `assessedOn`, `appraiser`, `sourceDocument` (`DocumentReference`) + `sourceContentHash` (idempotent import), `exchangeRate` (pinned RON/EUR — the rate moves between visits, so it belongs on the snapshot, not the catalog).

Owns a flat list of **`ConstructionValuationItem`** (local entities), every money field **computed once at capture, then frozen**:

- `valuationCatalogItemId` — kept only to (a) group the same item across snapshots for charting and (b) reach that item's BoQ links for the estimate-vs-real comparison.
- `name`, `estimatedValueWithoutVat`, `estimatedValueWithVat` — **denormalized** from the catalog item at capture, so a snapshot renders itself without joining a catalog that may since have changed (and survives the item's later `Deactivate`).
- `completionPercentage` (col H, appraiser's number).
- `completedValueWithoutVat`, `completedValueWithVat` (col I, with VAT).
- `remainingPercentage` (col J = 1 − H), `remainingValueWithoutVat`, `remainingValueWithVat` (col K).

**The core asymmetry:** catalog items recompute `totalCostWithVat` on a VAT change; snapshot items recompute **nothing** — that is the whole point of the split.

### Mapping of source spreadsheet columns

| Sheet | Column | Lands on |
| --- | --- | --- |
| Nr. Crt. (B col A) | A | `ValuationCatalogItem.printedNumber` (+ derived `sequence`) |
| Denumirea lucrării | B | `ValuationCatalogItem.name` |
| Sursa (Nr. Fișă) | C | `ValuationCatalogItem.catalogSource` |
| UM | D | `ValuationCatalogItem.unit` (raw text) |
| Cost lucrare (Lei/mpAd) | E | `ValuationCatalogItem.unitCostPerBuiltArea` |
| Pondere în total cost | F | `ValuationCatalogItem.costWeight` |
| Cost total, fără TVA | G | `ValuationCatalogItem.totalCostWithoutVat` |
| % executat | H | `ConstructionValuationItem.completionPercentage` |
| lei executați | I | `ConstructionValuationItem.completedValue…` (frozen) |
| % rămas | J | `ConstructionValuationItem.remainingPercentage` (frozen) |
| lei rămași | K | `ConstructionValuationItem.remainingValue…` (frozen) |
| Curs valutar (RON/EUR) | header | `ConstructionValuation.exchangeRate` |
| Cotă TVA | header | `ValuationCatalog.vatRate` |
| Suprafețe / ajustare regie | header | `ValuationCatalog` surfaces / `ownRegieAdjustment` |

### Application (read models)

Two cross-aggregate read-only queries (the domain holds only ids; money is composed at read time — the same pattern as `ProjectBudgetQuery` / `CostScenarioQuery`):

- **`ValuationVsBoqQuery`** (catalog-scoped, **live**): per catalog item, `totalCostWithoutVat` (estimate) vs. Σ of each linked `boq.SubtotalOf(section/subsection)`, each scaled by `boq.Multiplier(apartmentUnits)` and converted to RON via a supplied/pinned rate. **Net-to-net** (col G is VAT-exclusive; use `SubtotalOf`, not `SubtotalWithVatOf`); VAT-inclusive figures only at the project footer. Variance = actual − estimate (absolute + %). Because `SubtotalOf(sectionId)` already includes a section's direct lines *and* every subsection, a **whole-section** link gives full coverage, while **subsection** links cover only those subsections — any lines held **directly** in that section (not in a subsection) are covered by no link and must surface as a **coverage gap**, not silently vanish. Items with no links at all (the `%` catch-alls — *Alte lucrări de construcții*, *Alte instalații comune*, *Diverse, organizare, proiectare*) are likewise reported as coverage gaps ("X% of estimated value is mapped"), not as −100% variance.
- **`ValuationProgressQuery`** (snapshot-scoped, **frozen**): completed/remaining per item and project totals from a snapshot's stored values; and the same grouped by `valuationCatalogItemId` **across snapshots** for a progress chart.

Mutations go through app services that load via a repository port, invoke domain behaviour, and commit through the unit of work: `ValuationCatalogAppService` (create/edit catalog, add/revise/deactivate items, change VAT, link/unlink BoQ sections) and `ConstructionValuationAppService` (import/capture a snapshot idempotently, list). Snapshot capture reads the current catalog items' totals, freezes the derived values, and stores them.

### MCP

`McpServer/Tools/ValuationCatalogTools.cs` and `McpServer/Tools/ConstructionValuationTools.cs` expose the same operations to an agent — thin wrappers over the app-service and query ports, following the BoQ/cost-scenario precedent (the agent parses the source spreadsheet; the server only validates and persists). Grouped:

- **Catalog** (`IValuationCatalogAppService`): `get_project_valuation_catalog`, `get_valuation_catalog`, `create_valuation_catalog`, `update_valuation_catalog_header`, `activate_valuation_catalog`, `change_valuation_vat_rate`, `add_valuation_items` (bulk — the itemized estimate in one call; `unit` stays raw text), `revise_valuation_item`, `deactivate_valuation_item`, `link_valuation_item_to_boq`, `unlink_valuation_item_from_boq`, `delete_valuation_catalog`.
- **Snapshots** (`IConstructionValuationAppService`): `capture_construction_valuation` (idempotent by `sourceContentHash`; the agent supplies only each item's completion %, the RON/EUR rate is pinned as `ronPerEur`), `get_construction_valuation`, `list_construction_valuations`.
- **Read models**: `get_valuation_vs_boq` (`IValuationVsBoqQuery` — estimate vs. real; an optional `costScenarioId` switches the comparison from the default `Decided` basis to a `Scenario` basis, see [Valuation Comparison Basis](./valuation-comparison-basis.md)) and `get_valuation_progress` (`IValuationProgressQuery`).

BoQ section/subsection ids for `link_valuation_item_to_boq` come from the existing `get_boq` tool; a scenario id for the `Scenario` basis comes from `list_cost_scenarios`.

### Persistence (when implemented)

Tables `valuation_catalogs`, `valuation_catalog_items`, `valuation_item_links`, `construction_valuations`, `construction_valuation_items`. Unique index on `valuation_catalogs.projectId` (one per project). `valuation_item_links` carries a unique constraint on `(valuationCatalogId, boqId, sectionId, subsectionId)` to back the no-double-count invariant. Money stored as amount+currency; derived read-model figures are not persisted.

## Open Questions

- **Price revisions vs. history.** Decided: a revised report edits the catalog **in place** (frozen snapshots preserve history). Revisit only if a diff between two priced baselines is ever needed — that would reintroduce catalog versioning.
- **Denormalization drift.** A snapshot's `name`/`estimatedValue…` are copied at capture. If a catalog item is renamed/re-priced after a snapshot, the snapshot keeps the old values (intended). Confirm the UI signals that a snapshot reflects the catalog *as it was*.
- **Own-regie adjustment.** `ownRegieAdjustment` (0.20) is stored for provenance; whether any read model applies it to the estimate is unspecified — it appears already folded into the catalog totals in the source sheet.
- **Frontend & MCP surface** — delivered. The frontend (list/detail pages, vs-BoQ comparison, progress chart) and the MCP agent tools (see [MCP](#mcp)) both follow the BoQ/cost-scenario precedent.
