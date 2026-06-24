# BoQ Excel Export

- **Status:** Implemented <!-- Draft | In Review | Approved | Implemented | Deprecated -->
- **Author:** Serghei Grajdean
- **Date:** 2026-06-23
- **Related:** [Remote MCP Server](./remote-mcp-server.md), [Project Budget View](./project-budget-view.md), [Romanian Localization](./romanian-localization.md), [Domain Model](../architecture/domain-model.md), [Hexagonal Architecture](../architecture/hexagonal-architecture.md)

## Summary

Export a `BillOfQuantities` (a *deviz*) to a downloadable `.xlsx` workbook in which **each Section becomes its own worksheet** and **each Subsection is a visually separated band** within that worksheet, preceded by a summary sheet of section subtotals and the grand total.

## Motivation

The four stakeholders review and share priced *devize* outside the app — over email, with contractors, and against the source PDF. A spreadsheet is the lingua franca for that: it can be printed, annotated, filtered, and diffed against the contractor's original. Today the BoQ lives only inside the web UI. A faithful Excel projection — one that preserves the section / subsection / line-item structure and the domain-computed totals — makes the *deviz* portable without re-keying.

## Requirements

- [ ] `GET /api/bills-of-quantities/{id}/export` returns an `.xlsx` file (404 if the BoQ does not exist).
- [ ] The download filename is built from the **contractor name** and the **work-package name** of the owning bid (e.g. `SC Constructii SRL_Fundatie_2026-06-23.xlsx`), sanitized for the filesystem.
- [ ] Export is allowed in **every** `BoqStatus` (read-only; no mutability guard).
- [ ] The workbook opens with a **summary sheet** ("Rezumat"): a header block (reference, status, currency, submitted/valid dates) and one row per section with its subtotal, ending in a grand total.
- [ ] **One worksheet per Section**, ordered by `Sequence`, sheet name derived from the section name (sanitized to Excel's rules).
- [ ] Within a section sheet: a header, then the section's **direct** line items, then each **Subsection** rendered as a visually distinct band (shaded title row → its line items → its subtotal row), then a section total row.
- [ ] Each line-item row shows: index, description, unit code (U.M.), quantity, unit price (excl. VAT), VAT %, line total (excl. VAT), line total (incl. VAT).
- [ ] **Subtotals and totals are live Excel `SUM()` formulas** referencing the line-item cells, so the workbook recalculates if edited and is auditable against the domain figures.
- [ ] Labels and headers are in **Romanian**, matching the existing UI glossary.
- [ ] A download button on the BoQ detail page triggers the export through the Next.js server (since `API_BASE_URL` is server-only).

### Non-goals

- Editing or round-tripping: this is a one-way export. We do **not** import `.xlsx` back into the domain.
- PDF or CSV output (this spec is `.xlsx` only).
- Multi-BoQ / cross-bid workbooks — exactly one *deviz* per file.
- Styling beyond what conveys structure (banding, borders, number formats, frozen header). No theming/branding.

## Design

### Architecture placement (ports & adapters)

Spreadsheet generation is I/O, so it sits behind a **driven port** with an Infrastructure adapter; the domain and the math stay untouched. The domain already derives every monetary value — the export only renders and references those values via formulas.

| Layer | Component | Responsibility |
| --- | --- | --- |
| **Application / Abstractions** | `IBoqSpreadsheetExporter` (port), `BoqExportModel` (resolved read model), `BoqExportFile(byte[] Content, string FileName, string ContentType)` | Contract for rendering; the model the adapter consumes. |
| **Application / BillsOfQuantities** | `IBillOfQuantitiesAppService.ExportAsync(BoqId)` | Loads the BoQ, resolves units, builds `BoqExportModel`, invokes the port, returns the file. |
| **Infrastructure / Export** | `ClosedXmlBoqExporter : IBoqSpreadsheetExporter` | The only type that references ClosedXML. Registered in `AddInfrastructure()`. |
| **ApiService / Endpoints** | `GET /api/bills-of-quantities/{id}/export` | Thin handler → `Results.File(...)`. |
| **web** | `app/bills-of-quantities/[id]/export/route.ts` + a download button | Proxies the download and surfaces it in the UI. |

#### Why a resolved read model

`LineItemDto` carries `UnitOfMeasureId` (a `Guid`), not the unit code. The display column "U.M." needs `mc` / `buc` / `ml`, so `ExportAsync` loads units via `IUnitOfMeasureRepository`, builds a `Guid → Code` lookup, and bakes the codes into `BoqExportModel`. The exporter therefore needs **no** repository access and does pure rendering. Money values are passed through as the domain already computed them.

The model also carries the **contractor name** and **work-package name** for the filename. `ExportAsync` already has `IBidRepository`; it loads the owning `Bid` (via `BillOfQuantities.BidId`) to read its `ContractorId` and `WorkPackageId`, then resolves the two names through `IContractorRepository` and `IWorkPackageRepository` (both newly injected into the app service). A `Bid` references exactly one work package and one contractor, and there is one BoQ per bid — so this is a single contractor + single WP, not a list.

```csharp
// Application/Abstractions
public interface IBoqSpreadsheetExporter
{
    BoqExportFile Export(BoqExportModel model);
}

public sealed record BoqExportFile(byte[] Content, string FileName, string ContentType);

// BoqExportModel mirrors BillOfQuantitiesDto's tree but with unit codes resolved
// (sections → direct line items + subsections → line items), plus header fields
// (reference, status, currency, dates) and the single pricing currency.
```

`ExportAsync` uses the already-injected `TimeProvider` to stamp the filename date — no clock leaks into the domain.

### Library

**ClosedXML** (MIT), added through Central Package Management: a `<PackageVersion>` in the root `Directory.Packages.props` and a version-less `<PackageReference>` from **Infrastructure only**. ClosedXML is interop-free and ergonomic for named sheets, merged cells, fills, borders, number formats, frozen panes, and `SUM()` formulas. (EPPlus is license-viable for this noncommercial tool but ClosedXML avoids the licensing question entirely.)

### Workbook layout

#### Sheet 0 — "Rezumat" (summary)

```
Deviz: <Reference>        Status: <Status>       Monedă: RON
Depus: <SubmittedOn>      Valabil până: <ValidUntil>

 Secțiune                     Valoare (fără TVA)    Valoare (cu TVA)
 01 Terasamente                      12.500,00          15.125,00     ='01 Terasamente'!<totalCell>
 02 Structură                        48.300,00          58.443,00
 ───────────────────────────────────────────────────────────────
 TOTAL                               60.800,00          73.568,00     =SUM(above)
```

Each summary amount is a cross-sheet reference to that section's total cell; the TOTAL row is a `SUM()` over the section rows. The grand total thus reconciles with `BillOfQuantitiesDto.Total` / `TotalWithVat` by construction.

#### One worksheet per Section

Sheet name = `{Sequence:00} {Name}`, then sanitized: strip the Excel-illegal characters `: \ / ? * [ ]`, trim to ≤ 31 chars, and dedupe collisions with a numeric suffix.

```
 01 — Terasamente                                          ← merged title (bold)
 <Section.Description, if any>

 #  Descriere            U.M.  Cantitate  Preț unitar   TVA   Valoare      Valoare    ← frozen header row
                                          (fără TVA)     %    (fără TVA)   (cu TVA)
 1  Săpătură manuală     mc      120,00      45,00      21%    5.400,00    6.534,00   ← =qty*price ... =excl*1.21
 2  Sprijiniri maluri    mp       80,00      30,00      21%    2.400,00    2.904,00

 ▓ SUBCAPITOL: Epuismente ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  ← merged, shaded band row
 1  Pompare apă          oră      40,00      25,00      21%    1.000,00    1.210,00
    Subtotal subcapitol                                        1.000,00    1.210,00   ← =SUM(this band)

 ▓ SUBCAPITOL: Drenaje ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓
 1  Strat pietriș        mc      60,00      30,00      21%    1.800,00    2.178,00
    Subtotal subcapitol                                        1.800,00    2.178,00

 ═══════════════════════════════════════════════════════════
 TOTAL SECȚIUNE                                       10.600,00   12.826,00   ← =SUM(direct lines + subsection subtotals)
```

**Subsection separation** is achieved by, for each subsection in `Sequence` order: a blank spacer row, a full-width **merged, shaded band row** carrying the subsection name, the subsection's line items, then a bold **"Subtotal subcapitol"** row bordered off from the next band. The section's **direct** line items sit between the header and the first band. The bottom **"TOTAL SECȚIUNE"** row sums the direct line totals and the subsection subtotal rows.

#### Formulas

- Line total (excl. VAT) = `Quantity * UnitPrice` cell-referenced (not a literal).
- Line total (incl. VAT) = `excl * (1 + VatRate/100)`, with the per-row VAT cell referenced.
- Subsection subtotal, section total, summary rows = `SUM()` over the appropriate ranges.

Writing formulas (rather than the precomputed literals) keeps the sheet auditable and editable while still reconciling exactly with the domain figures on first open.

#### Formatting & Romanian labels

- Money: `#,##0.00` with the BoQ's currency; quantity `#,##0.00##`; VAT `0"%"`; `ro-RO` conventions.
- Frozen header row, bold/filled header, sensible column widths, borders delimiting bands and totals.
- Labels reuse the UI glossary: **Secțiune**, **Subcapitol**, **Descriere**, **U.M.**, **Cantitate**, **Preț unitar (fără TVA)**, **TVA %**, **Valoare (fără TVA)** / **Valoare (cu TVA)**, **TOTAL SECȚIUNE**, **Rezumat**, **Deviz**.

### Endpoint

```
GET /api/bills-of-quantities/{id:guid}/export
 200 → Results.File(content,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "<Contractor>_<WorkPackage>_<yyyy-MM-dd>.xlsx")   // filesystem-sanitized
 404 → BoQ not found
```

### Frontend download

`API_BASE_URL` is injected into the Next.js **server** only, so a browser `<a>` cannot reach the backend directly. A Next.js **route handler** at `app/bills-of-quantities/[id]/export/route.ts` fetches the file from `API_BASE_URL` server-side and streams it back with the `Content-Disposition` attachment headers. The BoQ detail page (`app/bills-of-quantities/[id]/page.tsx`) gets a plain link/button (`Descarcă Excel`) pointing at that route — no client-side `API_BASE_URL` exposure, consistent with how all other backend access is proxied.

### Edge cases

- **Empty section** (no direct lines, no subsections) → still gets a sheet, showing a zero section total.
- **Subsection with no lines** → band row + a zero subtotal.
- **Sheet-name collisions / illegal chars / > 31 chars** → sanitize + dedupe with a numeric suffix.
- **Unit id missing from the lookup** → blank U.M. (should not occur; line items must reference an active unit).
- Single `PricingCurrency` across the whole BoQ, so no cross-currency formatting is needed.

## Open Questions

- Filename uses `{Contractor}_{WorkPackage}_{date}`. Open: separator/format preference (underscore vs hyphen), and the fallback if a name is unexpectedly blank.
- Should the summary sheet also list each subsection (indented under its section), or sections only? (Current plan: sections only, to keep the overview compact.)
- Print setup (landscape, fit-to-width, repeated header rows) — include now or defer until someone actually prints?
