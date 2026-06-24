# BoQ Line-Item Reordering (drag & drop)

- **Status:** Implemented <!-- Draft | In Review | Approved | Implemented | Deprecated -->
- **Author:** Serghei Grajdean
- **Date:** 2026-06-24
- **Related:** [Domain Model](../architecture/domain-model.md), [BoQ Excel Export](./boq-excel-export.md), [UI Principles — Read-First](../guides/ui-principles.md)

## Summary

Let an editor reorder the line items of a Bill of Quantities (the contractor's *deviz*) by
drag-and-drop, including **moving a line from one subcapitol (subsection) to another** — or between
a section's direct lines and any subsection — anywhere within the same BoQ. Reordering is a deliberate
mode, not always-on, to preserve the read-first detail page.

## Motivation

A BoQ is structured `Section → (direct LineItems) + (Subsection → LineItems)`. Today every level
stores an `int Sequence`, but it is a free, non-contiguous integer set **by hand** through a numeric
input on each edit form, and applied only with `OrderBy` at read time. There is no move/reorder
operation at any layer, and a line item carries no parent pointer, so the only way to "move" a line
between subcapitols is to delete it and re-add it (minting a new id) and to manually renumber its
neighbours.

For a *deviz* extracted from a contractor PDF, the order frequently needs fixing and lines often
belong under a different subcapitol than where they landed. Hand-editing sequence numbers one row at
a time is tedious and error-prone. Direct manipulation (drag a row to its place) is the natural fit.

## Requirements

- [ ] Reorder line items within their current container (a section's direct list, or a subsection).
- [ ] Move a line item **across containers** — into any other subsection, or to/from a section's
      direct list — anywhere in the **same BoQ** (cross-section moves allowed).
- [ ] Moving preserves the line item's identity (`LineItemId`) and all its data (description,
      quantity, unit, price, VAT, notes).
- [ ] After any reorder/move, `Sequence` within each affected container is **dense and contiguous**
      (1..N), so the `#` column is meaningful and stable.
- [ ] Editing is gated by the existing rule: only a **Draft** or **Submitted** BoQ is mutable.
- [ ] The BoQ detail page stays read-first; drag-and-drop is available only in an explicit
      **"Arrange" mode** toggled on the page.
- [ ] Keyboard-accessible reordering (the four stakeholders include non-mouse use).

### Non-goals

- **Reordering subcapitols (subsections) or sections** — out of scope for this iteration. Their
  order keeps the current hand-set `Sequence` and edit forms. (Natural follow-up; the renumbering
  approach below generalises to them.)
- Unifying the two line-item tables into one. We deliberately keep `boq_line_items` and
  `boq_subsection_line_items` as-is (see Design → Persistence).
- Multi-select drag (moving several lines at once); one line per drag.
- Cross-**BoQ** moves (a line never leaves its BoQ).
- Undo/redo history beyond what re-dragging achieves.

## Design

### Domain (`HomeProjectManagement.Domain/BillsOfQuantities`)

Add **one** intention-revealing method on the `BillOfQuantities` root (a move can span two sections,
so it must live on the root, like the existing routing methods):

```csharp
/// <summary>
/// Move a line item to a target container (a section's direct list when
/// <paramref name="targetSubsectionId"/> is null, otherwise the named subsection) and place it at
/// <paramref name="targetIndex"/> (0-based, clamped to the container size). The line keeps its id and
/// data; the source and target containers are renumbered densely (1..N). No-op-safe within the same
/// container. Allowed only while the BoQ is editable.
/// </summary>
public void MoveLineItem(
    LineItemId lineItemId,
    SectionId targetSectionId,
    SubsectionId? targetSubsectionId,
    int targetIndex)
```

Behaviour:

1. `EnsureMutable()` (Draft/Submitted only — reuses the existing guard).
2. Locate the line and its current container by scanning sections and their subsections. Absent line
   or absent target → throw a domain exception (`DomainValidationException`, e.g. code
   `BoqLineItemNotFound` / `BoqTargetContainerNotFound`) so the app layer can map it to 404/422.
3. **Same container** (target section+subsection equals the source): reorder the existing instance in
   the backing list and renumber — *no remove/insert of the entity*. This matters for EF (see below).
4. **Different container**: detach the instance from the source list (EF delete), and insert a new
   `LineItem` constructed with the **same `LineItemId`** and the same data into the target list (EF
   insert). Currency is re-checked against the target's shared currency (always equal within a BoQ,
   but the guard stays).
5. Renumber both affected containers to a dense 1..N `Sequence`.
6. `Raise(new BoqLineItemMoved(Id, BidId, lineItemId, now))` — add the event record under
   `BillsOfQuantities/Events/`. `now` is passed in by the caller (no clock in the domain).

Supporting internal members (kept `internal`, driven by the root, mirroring the existing pattern):

- On `Section`: locate a line across its direct list and subsections; detach a line from the
  direct list or a named subsection; insert a (same-id) line into the direct list or a named
  subsection at an index; renumber a container. Section's *direct* list and each `Subsection` are the
  three container kinds the root addresses via `(SectionId, SubsectionId?)`.
- On `Subsection`: detach / insert-at / renumber for its own list.
- On `LineItem`: a `Sequence`-only setter is **not** added publicly; renumbering uses the existing
  `internal Revise(...)` (full data, new sequence) **or** a small dedicated `internal void Resequence(int)`
  to avoid re-passing all fields. Prefer adding `Resequence` — it's clearer and avoids currency
  re-validation churn.

> **Why detach + re-create rather than re-parent the instance.** A `LineItem` is mapped as an EF
> **owned** entity, and section-direct vs subsection lines are two *different* owned types in two
> tables. Moving the same tracked instance between owned collections risks EF identity-tracking
> conflicts. Re-creating with the same `LineItemId` makes the change unambiguous: delete from the old
> table, insert into the new. Within the *same* container we keep the instance and only update
> `Sequence`, avoiding a same-key delete+insert conflict. At this scale (small *devize*, 4 users) the
> extra delete/insert is irrelevant.

### Persistence (`Infrastructure/Persistence`)

No schema change. `boq_line_items` and `boq_subsection_line_items` stay as the two owned line-item
tables; a cross-subcapitol move becomes a row delete in one and an insert in the other within the
same `UnitOfWork`/transaction. `Sequence` columns already exist. **No migration required** — the
mapping is unchanged (the move/reorder logic is pure domain behaviour; the `BoqLineItemMoved` event
is `Ignore`d like the others), confirmed by a clean build with no model diff.

### Application (`Application/BillsOfQuantities`)

- New command:
  ```csharp
  public record MoveBoqLineItemCommand(
      Guid LineItemId,
      Guid TargetSectionId,
      Guid? TargetSubsectionId,
      int TargetIndex);
  ```
- New port method on `IBillOfQuantitiesAppService`:
  ```csharp
  Task<BillOfQuantitiesDto?> MoveLineItem(Guid boqId, MoveBoqLineItemCommand command, CancellationToken ct);
  ```
  Implementation: load the aggregate (404 → `null`), call `MoveLineItem(...)` with `now` from
  `TimeProvider`, `CommitAsync()`, and return the **full re-projected `BillOfQuantitiesDto`** so the
  client reconciles to the canonical, renumbered order. Domain validation/conflict exceptions surface
  through the existing exception-to-HTTP mapping.

### API (`ApiService/Endpoints/BillOfQuantitiesEndpoints.cs`)

One surgical endpoint per drop:

```
POST /api/bills-of-quantities/{id}/move-line-item
Body: { lineItemId, targetSectionId, targetSubsectionId?, targetIndex }
200 → updated BillOfQuantities (renumbered)
404 → BoQ or line item not found
409 → BoQ not editable (BoqNotEditable)
422 → invalid target container / index
```

Index-based and order-canonicalising, so a retried or stale drop converges rather than corrupting
order.

### Frontend (`src/web/app`)

- **Library:** add `@dnd-kit/core` + `@dnd-kit/sortable` (modern, maintained, keyboard + screen-reader
  support). `react-beautiful-dnd` is archived — do not use. Add via `npm install` in `src/web`
  (consult `src/web/node_modules/next/dist/docs/` per the Next 16 warning before writing client code).
- **Arrange mode:** add an "Arrange / Aranjează" toggle to
  `app/bills-of-quantities/[id]/page.tsx`. Read mode renders today's static `LineItemsTable`
  unchanged. Arrange mode renders a **client** component that wraps the *entire BoQ* in a single
  `DndContext` (one context spanning all sections and subsections so a line can be dragged anywhere),
  with each container a `SortableContext`. Rows get a drag handle; the table otherwise mirrors the
  read layout.
- **On drop:** compute `(targetSectionId, targetSubsectionId?, targetIndex)` from the destination
  container and position, optimistically reorder local state, then call a new server action
  `moveLineItem(...)` in `app/bills-of-quantities/actions.ts` → the move endpoint → `revalidatePath`.
  Reconcile local state from the returned BoQ (authoritative `Sequence`s). On error, revert and show
  a message.
- **i18n:** add the "Arrange" label and any drag a11y strings to `app/lib/i18n/ro.ts`.
- The static read-mode `#` column and the manual `sequence` number inputs on the edit forms can stay
  for now; once Arrange mode lands, the manual sequence inputs become redundant and may be removed in
  a follow-up.

### Flow (cross-subcapitol move)

```
User (Arrange mode): drag line L from subsection A into subsection B, drop at row 2
  → client: optimistic reorder; POST /bills-of-quantities/{id}/move-line-item
            { L, sectionId(B), subsectionId(B), targetIndex: 1 }
  → app:    load BoQ → boq.MoveLineItem(L, secB, subB, 1, now)
                detach L from A (delete row in boq_subsection_line_items)
                insert L' (same id) into B (insert row)         renumber A and B → 1..N
            → CommitAsync (one transaction) → re-project DTO
  → client: reconcile to returned order
```

## Open Questions

- Should the manual `sequence` number inputs on the line-item edit forms be **removed** once Arrange
  mode ships, or kept as a fallback? (Leaning: remove in a follow-up to avoid two ways to set order.)
- Reordering **subcapitols and sections** is the obvious next step — confirm it's a separate spec
  rather than folded in here.
- Concurrency: with four users, do we need optimistic-concurrency protection on the BoQ for
  simultaneous arranges, or is last-write-wins acceptable? (Leaning: last-write-wins; the endpoint
  returns canonical state.)
