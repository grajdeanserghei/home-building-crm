# Valuation Comparison Basis (Estimate vs. Real over competing BoQs)

- **Status:** Draft <!-- Draft | In Review | Approved | Implemented | Deprecated -->
- **Author:** Serghei Grajdean
- **Date:** 2026-07-15
- **Related:** [Construction Valuation](./construction-valuation.md), [Cost Scenarios](./cost-scenarios.md), [Project Budget View](./project-budget-view.md), [Domain Model](../architecture/domain-model.md)

## Summary

Correct the **estimate-vs-real** read model so it treats a project's bills of quantities as **competing offerings** rather than additive parts. An appraiser's valuation item may be assigned to a section in **several competing BoQs** (one per bidding contractor); those assignments are **alternatives**, so the "real" cost must **select one BoQ per work package** — it must never sum them. Selection is an explicit, first-class **comparison basis**, defaulting to the work package's *decided* BoQ (accepted contract, else the selected bid), with a **scenario** basis for the cost simulator's what-if.

## Motivation

The first cut of `ValuationVsBoqQuery` computes an item's actual cost as **Σ of every mapped BoQ subtotal**, regardless of which BoQ each link points at. That is wrong once BoQs are understood correctly:

- A work package receives **many bids**, each with its **own BoQ** — these are **competing offerings**, mutually exclusive alternatives.
- The owners map the **same** appraiser item onto the corresponding section in **each** competing BoQ (Contractor A's "Structură" *and* Contractor B's "Structură" both carry *Beton armat*).
- Summing both prices **double-counts** one physical scope of work and produces a meaningless "real" figure.

An item's links therefore **partition by work package**:

- links to BoQs of the **same** work package are **mutually exclusive alternatives** — exactly one is "real";
- links to BoQs of **different** work packages are **additive** — the item genuinely spans them.

"Which competing BoQ is real?" must become an explicit criterion, not an accident of summation. There are two legitimate answers the owners want:

1. **What did we actually commit to?** — the work package's *decided* BoQ (the accepted/awarded one, or the selected bid before award). This is the standalone "Estimat vs. real".
2. **What if we picked this combination?** — a cost scenario's selection, for the simulator.

## Requirements

- [ ] An appraiser item's actual cost **selects one BoQ per work package** among its competing links; it never sums competing BoQs of the same work package.
- [ ] Actual cost **sums across different work packages** an item spans.
- [ ] "Which BoQ is real" is an explicit **comparison basis**:
  - [ ] **Decided** (default): the work package's **accepted** BoQ (via its awarded contract); if none, its **selected** bid's BoQ; if neither, the item is **not realized** for that work package.
  - [ ] **Scenario**: a `CostScenario`'s chosen bid per work package (what-if for the simulator).
- [ ] Money stays **net-to-net**, scaled to the whole build (`× boq.Multiplier(apartmentUnits)`) and converted to the catalog currency — the existing basis of the read model.
- [ ] Coverage is reported honestly per basis: an item whose links point only at **non-active** BoQs is a **`NotRealized`** gap (not a −100% variance); an item with **no** links is an `UnmappedItem` gap (unchanged); subsection-partial coverage of an active section is an `UnattributedBoqLines` gap (unchanged, restricted to active BoQs).
- [ ] The project-wide comparison and the cost-scenario page share **one** query and one DTO, differing only by basis.

### Non-goals

- **No new persisted state for the basis.** *Decided* and *Scenario* are both derivable from existing aggregates (`WorkPackage.AwardedContractId → Contract.AcceptedBoqId`; the `Selected` `Bid`; `CostScenario` selections). No "reference BoQ" is stored.
- **No change to the mapping's granularity rules** (no-double-count per `(boqId, sectionId, subsectionId)`; whole-section vs subsection exclusivity) — those stand.
- **No cross-currency grand total** (RON and EUR are never summed — the `Money` rule).
- **The domain does not traverse to bids/contracts** — resolving the active BoQ is application work (ports and adapters).

## Design

### Domain — make the competing group explicit on the link

Enrich the owned value object:

```
ValuationItemLink { BoqId, WorkPackageId, SectionId, SubsectionId? }
```

`WorkPackageId` is **populated by the application service at link time**, exactly like the existing resolution of a subsection link's *actual* parent `SectionId`: `LinkBoqSectionAsync` already loads the target BoQ to validate the link, so it also resolves `boq → bid → workPackageId` and stamps it on the link. The domain stays clock-free and I/O-free; it merely stores the tuple.

This turns "these links compete" into a **queryable fact** (`GROUP BY WorkPackageId`) instead of a two-hop re-derivation at read time, and it keeps the `ValuationCatalog` aggregate self-contained (it never references a `Bid` or `Contract`).

**Invariants unchanged.** No-double-count (`(BoqId, SectionId, SubsectionId)` → at most one item) and granularity exclusivity remain. We deliberately **allow** multiple links per work package on one item — that is precisely how a competing assignment is expressed. The competing-vs-additive semantics live in the *read model*, not as a blocking write invariant, because "which is active" is a read-time choice.

### Application — the comparison basis and the real-BoQ selector

A driven port resolves one active BoQ per work package for a basis:

```csharp
// Application/Valuations
public abstract record ComparisonBasis
{
    public sealed record Decided : ComparisonBasis;                 // accepted-then-selected
    public sealed record Scenario(Guid CostScenarioId) : ComparisonBasis;
}

public interface IRealBoqSelector
{
    // WorkPackageId -> the active (real) BoqId for that work package under this basis.
    Task<IReadOnlyDictionary<Guid, Guid>> ResolveAsync(
        Guid projectId, ComparisonBasis basis, CancellationToken ct);
}
```

- **Decided** — for each project work package: `AwardedContractId → Contract.AcceptedBoqId`; else the work package's `Selected` bid's BoQ; else no entry (nothing realized).
- **Scenario** — read the `CostScenario`'s selections (workPackage → bid → BoQ), the same resolution `CostScenarioQuery` already performs.

`ValuationVsBoqQuery` is refactored to a shared rollup parameterised by the resolved active set:

```
active = selector.Resolve(projectId, basis)                 // workPackageId -> active BoqId

actual(item) = Σ over link in item.Links
                 where active[link.WorkPackageId] == link.BoqId
                 of  convert(scale(boq.SubtotalOf(section | subsection)))
```

Because at most one BoQ per work package is active, an item's competing links **collapse to a single contribution per work package**; summation happens only **across** work packages — double-counting is impossible by construction. Per-link contributions stay in `links[]` (with `boqResolved`) for a future breakdown view.

The existing project-wide entry point switches its default to **`Decided`**, so the standalone comparison stops double-counting immediately.

### Application → API

The read model DTO is reused as-is (`ValuationVsBoqDto`), plus a `NotRealized` coverage-gap kind and, optionally, the resolved `Basis` echoed back for display.

- `GET /api/projects/{projectId}/valuation/comparison?basis=decided` — standalone (default `decided`).
- `GET /api/cost-scenarios/{scenarioId}/valuation-comparison` — scenario basis; project + catalog resolved from the scenario.

### Frontend

- `getValuationComparison(projectId)` keeps its shape (now *Decided*-based); add `getScenarioValuationComparison(scenarioId)`.
- Extract a shared `ValuationComparisonTable` component from the current `/valuation/vs-boq` page (per-item table + totals footer + coverage gaps), reused by both the standalone page and an **"Estimat vs. real"** card on `/projects/[id]/cost-scenarios/[scenarioId]`. Because `setScenarioSelection` already `revalidatePath`s the scenario page, changing a bid re-runs the scenario-scoped comparison for free.
- i18n: reuse `valuation.vsBoq.*`; add the card title and the `NotRealized` gap label.

### Migration & impact

- Domain/persistence: add `WorkPackageId` to `ValuationItemLink` + its EF configuration → **one migration** with a **backfill** that resolves `boq → bid → workPackageId` for existing links.
- Application: new `IRealBoqSelector` (Decided/Scenario adapters); `LinkBoqSectionAsync` stamps `WorkPackageId`; `ValuationVsBoqQuery` refactored group-then-pick.
- API/frontend: one new endpoint, one shared table component, one scenario-page card.

## Open Questions

- **Selected-bid ambiguity.** A work package can technically have a `Selected` bid whose BoQ is *not* `Accepted`. *Decided* prefers the accepted BoQ, then the selected bid's BoQ — confirm that precedence is desired (vs. requiring an accepted BoQ before an item counts as "real").
- **Manual override basis.** A `SpecificBoqs` basis (hand-pick the real BoQ per work package, independent of award/selection) is easy to add later but omitted now — is it ever needed outside the scenario what-if?
- **Inverse coverage.** Real BoQ cost under an active BoQ that **no** appraiser item maps to (real cost with no estimate) is currently invisible in the per-item view. Worth a project-level "unattributed real cost" line, or out of scope?
