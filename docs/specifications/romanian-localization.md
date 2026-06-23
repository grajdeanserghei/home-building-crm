# Romanian Localization (Romanian-only UI)

- **Status:** Implemented <!-- Draft | In Review | Approved | Implemented | Deprecated -->
  <!-- Code for all phases is in (see "Implementation" below). Glossary/enum terms marked
       **⚠ ratify** are shipped with the proposed Romanian but still await stakeholder sign-off. -->
- **Author:** Serghei Grajdean
- **Date:** 2026-06-22
- **Related:** [`docs/architecture/domain-model.md`](../architecture/domain-model.md), [`docs/architecture/hexagonal-architecture.md`](../architecture/hexagonal-architecture.md), [`docs/project-overview.md`](../project-overview.md)

## Summary

Present the entire application in **Romanian** — UI copy, enum labels, dates/numbers/currency formatting, and user-facing error messages. This is a **presentation-layer** effort: the domain, persistence, and wire contracts stay in English/invariant; only what a stakeholder reads is translated. The single most important artifact is the **glossary** below, which fixes the Romanian ubiquitous language so terminology is consistent everywhere.

This spec is **Phase 1**: the agreed glossary and the localization design. The code changes (formatting layer, message catalog, string extraction, error codes) are separate phases that all reference the tables here.

## Motivation

The four stakeholders building the duplex are Romanian speakers, and the data they coordinate is already Romanian (project names, discussion notes, PDF *devize*, line-item descriptions). An English UI is friction for everyone except the developer. Because this is a private, four-person internal tool, there is no need for a language switcher — Romanian-only removes the cost of dual catalogs, a switcher, and locale routing, while still giving us one reviewed source of truth for construction terminology.

Two forces make a *reviewed glossary* (not ad-hoc choices in JSX) the heart of this work:

- The app has a deliberate **ubiquitous language** (`BillOfQuantities`, `WorkPackage`, `Bid`, …). The Romanian equivalents are real, specific construction terms, and the stakeholders are the domain experts who must ratify them.
- Enum *names* are the persistence and JSON wire contract (`HasConversion<string>` + `JsonStringEnumConverter`), so Romanian labels **cannot** be a rename — they must live in a presentation map keyed by the English name.

Goals:

- Every user-facing string, enum label, date, number, and currency renders in Romanian / `ro-RO`.
- A single glossary governs domain terminology; one catalog file holds all UI copy.
- Domain rule violations reach the user in Romanian, **without** dragging localization infrastructure into the pure domain core.

### Non-goals

- **No bilingual switcher.** Romanian-only. (Structured so a future `en` catalog is additive, not a rewrite — see Design.)
- **No translation of user-entered data.** Free-text (project names, notes, descriptions) is stored and displayed exactly as typed.
- **No server-side `RequestLocalization` / `.resx` / `IStringLocalizer`.** Domain messages stay English on the wire; the frontend translates them via stable codes (see Design → Error messages).
- **No change to enum members, persisted values, or JSON contracts.** English names remain the contract.

## Requirements

- [ ] A ratified Romanian glossary for every domain term (table below).
- [ ] Romanian labels for every member of every domain enum (tables below).
- [ ] `ro-RO` formatting for dates, numbers, percentages, and currency, applied through a single shared helper module (eliminating the ~10 duplicated `formatDate` copies and the locale-less `formatMoney`).
- [ ] A single Romanian message catalog (`ro`) + a thin `t(key, params?)` helper; no inline UI literals remain in pages/components.
- [ ] `<html lang="ro">` and Romanian `<title>`/metadata.
- [ ] Domain exceptions carry a stable `Code` (+ optional params); `DomainExceptionHandler` surfaces them as ProblemDetails extensions; the frontend renders Romanian for known codes and falls back to the English `Detail` for the long tail.
- [ ] Backend numeric/date `ToString()` that reaches persistence or the wire is pinned to `InvariantCulture`.

### Non-goals (scope guard)

- Multilingual data, per-user language preference, RTL, or pluralization beyond Romanian's needs.

## Design

### Principles

1. **Presentation-only.** Domain, EF Core, and JSON stay English/invariant. Romanian is applied at render time (frontend) and at error-render time (frontend, via codes).
2. **Enum names are the contract.** Romanian enum labels live in a presentation map keyed by the English member name. The wire still carries `"InProgress"`; the screen shows `"În desfășurare"`.
3. **One glossary, one catalog.** Domain terms come from the glossary; all UI copy lives in a single `ro` message file so the four stakeholders can review terminology in one place.
4. **Invariant on the wire, `ro-RO` on screen.** Any backend `ToString()` feeding persistence/JSON uses `InvariantCulture`; all human-facing formatting happens on the frontend with `Intl.*('ro-RO')`.
5. **Additive bilingual escape hatch.** Even though we ship Romanian-only, copy lives in a `ro` catalog (not hardcoded), so adding `en` + a switcher later is incremental rather than a rewrite.

### Ubiquitous-language glossary

Romanian terms for the domain language. Entries marked **⚠ ratify** are where local construction usage matters most — please confirm or correct.

| English (code) | Romanian | Notes |
|---|---|---|
| Project | Proiect | |
| Work Package | Pachet de lucrări | |
| Scope item | Articol de scop | **⚠ ratify** — what is this called on site? (e.g. *Element de scop*, *Cerință*) |
| Contractor | Antreprenor | alt. *Executant* (contract-speak) |
| Bid | Ofertă | |
| Bill of Quantities (BoQ) | Listă de cantități | **⚠ ratify** — *Listă de cantități* vs *Antemăsurătoare* vs *Deviz* (the existing MCP spec uses *deviz* for the priced PDF) |
| Section (of a BoQ) | Secțiune | |
| Line item | Articol | alt. *Poziție* (de deviz) |
| Contract | Contract | |
| Unit of Measure | Unitate de măsură | abbr. *U.M.* |
| Discussion note | Notă de discuție | |
| Exchange rate | Curs valutar | |
| VAT | TVA | |
| VAT rate | Cotă TVA | |
| Money / Amount | Sumă | |
| Due date | Termen | alt. *Dată-limită* |
| Created / Updated | Creat / Actualizat | audit fields |

### Enum labels

Keyed by English member name (the wire value). One presentation map per enum.

**ProjectStatus**

| Member | Romanian |
|---|---|
| Planned | Planificat |
| InProgress | În desfășurare |
| OnHold | Suspendat |
| Completed | Finalizat |

**WorkPackageStatus**

| Member | Romanian |
|---|---|
| Defined | Definit |
| OpenForBids | Deschis pentru oferte |
| Awarded | Atribuit |
| InProgress | În desfășurare |
| Completed | Finalizat |
| Cancelled | Anulat |

**ScopeItemRequirement**

| Member | Romanian |
|---|---|
| Mandatory | Obligatoriu |
| Optional | Opțional |

**BidStatus**

| Member | Romanian | Notes |
|---|---|---|
| InDiscussion | În discuție | |
| BoqExpected | Deviz așteptat | the contractor committed to send a priced BoQ (*deviz*) |
| BoqReceived | Deviz primit | a priced BoQ arrived — supersedes the former `Quoted` |
| Shortlisted | Preselectat | |
| Selected | Selectat | |
| Rejected | Respins | |
| Withdrawn | Retras | |

> Note: `BidStatus` was refactored after this spec's first draft — the former `Quoted` member
> was split into `BoqExpected` / `BoqReceived`. The labels above reflect the current enum.

**NoteType**

| Member | Romanian |
|---|---|
| Meeting | Întâlnire |
| Call | Apel telefonic |
| Email | Email |
| Note | Notă |

**BoqStatus**

| Member | Romanian | Notes |
|---|---|---|
| Draft | Ciornă | alt. *Schiță* |
| Submitted | Trimis | alt. *Depus* |
| Accepted | Acceptat | |
| Rejected | Respins | |
| Withdrawn | Retras | |

**ContractStatus**

| Member | Romanian |
|---|---|
| Draft | Ciornă |
| Signed | Semnat |
| Active | Activ |
| Completed | Finalizat |
| Terminated | Reziliat |

**UnitCategory**

| Member | Romanian | Notes |
|---|---|---|
| Length | Lungime | |
| Area | Suprafață | alt. *Arie* |
| Volume | Volum | |
| Mass | Masă | |
| Count | Bucăți | countable units (buc.) |
| Time | Timp | |
| Other | Altele | |

**Currency** — ISO 4217 codes are kept as-is (`RON`, `EUR`); they are not translated. Display may render `RON` as `lei` — see formatting.

### Formatting (`ro-RO`)

All human-facing formatting goes through one shared frontend helper module (replacing the duplicated `formatDate` and the hand-rolled `formatMoney`):

| Kind | Rule | Example |
|---|---|---|
| Date (`DateOnly`, e.g. due date) | `Intl.DateTimeFormat('ro-RO')` | `22.06.2026` |
| Timestamp (`DateTimeOffset`) | `Intl.DateTimeFormat('ro-RO', { dateStyle, timeStyle })` | `22.06.2026, 14:30` |
| Number | decimal comma, dot grouping | `12.500,50` |
| Currency | `Intl.NumberFormat('ro-RO', { style: 'currency', currency })` | `12.500,50 RON` |
| Percentage (VAT) | `…,##%` | `21%` |
| Null | `—` (em dash), as today | |

Backend: `Money.ToString()` and `VatRate.ToString()` currently use the **ambient thread culture** — pin these to `CultureInfo.InvariantCulture` so persisted/logged/wire output is stable regardless of server locale. Human display stays on the frontend.

> **Open decision:** show currency as the ISO code (`RON`) or the symbol (`lei`)? `Intl` defaults to `RON`. Noted in Open Questions.

### UI copy catalog

- A single dictionary (`src/web/app/lib/i18n/ro.ts` or `ro.json`) holds all UI strings, keyed by stable dot-notation keys (e.g. `projects.title`, `lineItem.unitPriceExclVat`, `common.noResults`).
- A thin `t(key, params?)` helper does lookup + `{placeholder}` interpolation. No i18n library is required for Romanian-only; the helper is a few lines. (If English returns later, this becomes a `messages[locale]` lookup — additive.)
- The enum maps in `app/lib/api.ts` (`PROJECT_STATUS_LABELS`, etc.) are repointed at the catalog's Romanian values rather than holding English.
- `app/layout.tsx`: `<html lang="ro">`, Romanian `<title>` and metadata.

### Error messages (codes, not server-side localization)

The domain stays pure; the frontend translates.

1. `DomainException` (and subclasses `DomainValidationException`, `DomainConflictException`) gain a stable **`Code`** (e.g. `ScopeItemNameDuplicate`) and an optional **params bag** for interpolated values (e.g. `{ name }`). The English `Message` is retained as the developer-facing fallback.
2. `DomainExceptionHandler` surfaces `code` and `params` as ProblemDetails **extensions**, alongside the existing `title`/`detail`/`parameter`.
3. The frontend maps known `code`s to Romanian templates (re-interpolating `params`), and falls back to the English `Detail` for codes it doesn't yet translate. This means partial coverage is safe — untranslated errors degrade to English rather than breaking.

> Interpolated messages (e.g. `$"A scope item named '{name}' already exists…"`) **must** pass `name` as a param, not bake it into the string — otherwise the Romanian template can't reconstruct the sentence. Codes/params are introduced incrementally, highest-frequency violations first.

### Phasing (informative)

1. **This spec** — ratify glossary + enum labels.
2. Formatting helper module + `InvariantCulture` backend fix (standalone improvement).
3. `ro` catalog + `t()` helper; repoint enum maps.
4. Extract inline literals across the ~20 pages/components.
5. Error `Code`/params on `DomainException` + handler + frontend code map.

## Open Questions

- **Bill of Quantities** term: *Listă de cantități*, *Antemăsurătoare*, or *Deviz*? (The MCP spec already uses *deviz* for the priced PDF — do BoQ and *deviz* mean the same thing here, or is *deviz* specifically the priced/submitted variant?) *Shipped as:* glossary term **Listă de cantități**, with **deviz** used for a specific priced version on the bid/BoQ screens — confirm this split.
- **Scope item** — confirm the on-site term. *Shipped as:* **Articol de scop**.
- **BidStatus** — `Quoted` no longer exists (see enum table); confirm **Deviz așteptat** / **Deviz primit**.
- **Currency display** — ISO code (`RON`) or symbol (`lei`)? *Shipped as:* **ISO code** (`Intl.NumberFormat('ro-RO', { currencyDisplay: 'code' })` → `12.500,50 RON` / `12.500,50 EUR`), for consistent RON/EUR rendering. Switch to symbol later by dropping `currencyDisplay`.
- Any term where the team already has an established habit that should override the proposals above.

## Implementation

All phases are implemented. Presentation-only: the domain, EF Core and JSON wire stay English/invariant.

**Backend** (`src/HomeProjectManagement.*`):
- `Money.ToString()` / `VatRate.ToString()` pinned to `CultureInfo.InvariantCulture`.
- `DomainException` gained a stable `Code` + `Parameters` bag (interpolated values passed as params, not baked into the message); `DomainValidationException` / `DomainConflictException` thread them through. `DomainExceptionHandler` surfaces `code` and `params` as ProblemDetails extensions. Codes assigned so far: `ScopeItemNameDuplicate`, `BoqClosed`, `BoqNotEditable`, `BoqExchangeRateCurrencyMismatch`, `LineItemCurrencyMismatch`, `BidInvalidStatusTransition`, `ContractClosed` (more added incrementally; untranslated codes fall back to the English `detail`).

**Frontend** (`src/web/app`):
- `lib/i18n/ro.ts` — the single Romanian catalog (glossary, enum labels, common UI, nav/metadata, error templates, per-feature copy). `lib/i18n/index.ts` — the `t(key, params?)` helper.
- `lib/format.ts` — shared `ro-RO` `formatDate` / `formatDateTime` / `formatNumber` / `formatMoney` / `formatPercent` (replaced ~11 duplicated `formatDate` copies and the locale-less `formatMoney`).
- `lib/errors.ts` — `describeApiError(res)` maps a ProblemDetails `code`+`params` to a Romanian template, falling back to the English `detail`.
- The enum label maps in `lib/api.ts` are repointed at the catalog; `app/layout.tsx` sets `<html lang="ro">` + Romanian metadata and loads the `latin-ext` font subset for diacritics. All page/component inline literals were extracted to `t()`.
