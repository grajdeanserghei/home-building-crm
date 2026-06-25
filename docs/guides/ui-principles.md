# UI Principles — Read-First

These principles govern the Next.js frontend (`src/web`). They exist to correct a
specific drift: the app had become dominated by large Add/Create forms rendered at the
top of list pages, so the first thing a stakeholder saw when they came to *read* their
data was a form to *write* new data. Reading is the primary job of this tool — four
people coordinating a build need to see status, costs, and contacts at a glance far more
often than they create new records.

**The rule that subsumes the rest: a page's default state is reading. Writing is a
deliberate, separate act the user opts into.**

`app/projects/page.tsx` is the reference implementation of everything below. When in
doubt, copy its shape. `app/contractors/page.tsx`, `units-of-measure`, and `trades`
still embed a create form above the list — these are the cases to migrate, not to imitate.

## The principles

### 1. Read is the default state of every page
Opening any resource shows its data, formatted for scanning. No list or detail page
greets the user with a form. If the first paint is dominated by empty inputs, the page
is wrong.

### 2. Creating is a destination, not a section of the list
"Add" is a single primary action in the page toolbar that navigates to a dedicated route
(`/{resource}/new`), mirroring the existing edit-route convention (`/{resource}/{id}/edit`).
The list page's only job is to list. Do **not** render a `<XForm>` inline above the table.

### 3. One primary action per view
The toolbar (title + subtitle on the left, action on the right) holds exactly **one**
prominent button — the create CTA (`styles.primaryButton`). Everything else is quieter.
Two loud buttons compete and erase the hierarchy that makes a page readable.

### 4. Per-row actions stay quiet and consistent
The record's **name is the link to its read (detail) view**. Edit and delete are
low-emphasis, right-aligned, and appear in the same order on every table. They never
out-shout the data in the row.

### 5. Editing is a step away from reading, never inline-by-default
Detail pages show data. An **Edit** button routes to the edit form
(`/{resource}/{id}/edit`), which reuses the same `<XForm>` component the `/new` route
uses. Detail pages do not render editable fields by default.

### 6. Surface signal before rows
Where it aids reading, a compact summary/stats strip sits between the title and the
table — computed from already-loaded data, with **no extra request** (see the
`countByStatus` reduce in `projects/page.tsx`). Give the reader the headline before the
detail.

### 7. The empty state is the *only* time the create CTA gets loud
When a list has zero rows, show an inviting empty state that includes the create action —
that is the one moment creation is the user's likely intent. Once data exists, the single
toolbar button is enough; demote creation and let the data fill the page.

### 8. Density and consistency over chrome
Tables are formatted for comparison: aligned columns, muted secondary text
(`styles.muted` for descriptions/notes under the name), status badges, consistent date
formatting via `formatDate`. Keep column order and formatting consistent across
aggregates so the reader learns one layout, not eight.

### 9. Progressive disclosure for nested data
Owned/child collections (BoQ sections → subsections → line items, bid notes) show a
summary first and reveal detail on navigation or expansion. Don't flatten a whole tree of
write affordances onto one read surface.

### 10. Every delete is confirmed before it happens
Deletion is irreversible and must never fire on a single click. Every delete button
opens a confirmation dialog that names what is about to be removed and requires an
explicit confirm before the action runs. This applies to all destructive actions —
per-row deletes, detail-page deletes, and any "remove" of nested/child data — not only
top-level records.

## The one sanctioned inline mutation
A single-field, low-risk toggle may mutate in place rather than routing to a form — e.g.
`UnitOfMeasureActiveToggle` and `TradeActiveToggle`. This is the deliberate exception, not
a license to inline full forms. The test: it changes exactly one boolean, needs no
validation, and the user stays in their reading flow.

## Mechanics (how these are implemented today)
- Forms live on their own routes (`/new`, `/{id}/edit`) and reuse one `<XForm>` component
  for both create and edit.
- Mutations are React Server Actions in `app/actions.ts` (and per-resource `actions.ts`),
  which `revalidatePath(...)` after a write and redirect back to the read view.
- Toolbar, card, table, stat, badge, and muted styles already exist in `page.module.css`;
  use them rather than introducing new layout primitives.

## Checklist for any new list/detail page
- [ ] First paint is data, not a form.
- [ ] Exactly one primary action (Add → `/new`) in the toolbar.
- [ ] Row name links to the detail/read view; edit & delete are quiet, right-aligned.
- [ ] Every delete button opens a confirmation dialog before removing anything.
- [ ] Editing routes to `/{id}/edit`, reusing the create form component.
- [ ] Empty state carries the create CTA; populated state does not repeat it loudly.
- [ ] Any summary strip is computed from already-loaded data (no extra fetch).
