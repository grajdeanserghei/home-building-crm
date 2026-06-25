# Cost Scenarios (Cost Simulator)

- **Status:** Implemented <!-- Draft | In Review | Approved | Implemented | Deprecated -->
- **Author:** Serghei Grajdean
- **Date:** 2026-06-25
- **Related:** [Project Budget View](./project-budget-view.md), [Domain Model](../architecture/domain-model.md), [Remote MCP Server](./remote-mcp-server.md)

## Summary

A **Cost Simulator** that lets the owners build and save named "what-if" cost combinations for a project: for each work package they pick **one bid** (its bill of quantities), and the combined cost is computed on the fly — per currency, with an approximate EUR-equivalent. Each saved combination is a **`CostScenario`**.

## Motivation

The existing [Project Budget View](./project-budget-view.md) is rule-driven: per work package it shows the *range* of all received bids and cannot express a single chosen combination. The owners want to answer "what will the build cost if we award **this** bid for the foundation, **that** bid for the roof…?" and keep several such combinations side by side to compare. That is a saved, user-chosen selection — distinct from the budget's automatic rollup.

## Requirements

- [x] Save named scenarios per project (a persisted aggregate, not ephemeral).
- [x] Selection model: at most **one bid per work package** (an award what-if; prevents double-counting).
- [x] Candidate bids offered are the current project's **priced** BoQs (not rejected/withdrawn, positive total).
- [x] Output: a per-work-package breakdown plus per-currency net/gross totals and an approximate EUR-equivalent.
- [x] Figures scaled to the whole build — a per-apartment quote × the project's `apartmentUnits`.
- [x] Reachable from the project detail page; CRUD over HTTP and via the MCP server.

### Non-goals

- No combining of BoQs across projects (a scenario is scoped to one project).
- No saved comparison/diff between two scenarios (they are listed; comparison is visual).
- No cross-currency grand total — RON and EUR are never summed (the `Money` value object forbids it); an approximate EUR-equivalent is shown separately.

## Design

### Aggregate (`CostScenario`)

A new aggregate root under `Domain/CostScenarios/`. It references its `ProjectId` by id and owns a set of **`ScenarioSelection`** entities (each holding a `WorkPackageId` + `BidId` by id), with at most one selection per work package enforced in the domain and by a composite database key. It holds **only ids** — no money. `IncludeBid(workPackageId, bidId)` upserts a selection; `RemoveWorkPackage(workPackageId)` drops one. Cross-aggregate validity (the work package belongs to the project, the bid belongs to that work package) is checked by the application service, not the domain.

### Application

- **`CostScenarioAppService`** — mutations + the summary list (create/update/include-bid/remove-work-package/delete), each loading via the repository port, invoking domain behaviour, and committing through the unit of work.
- **`CostScenarioQuery`** — read-only cross-aggregate composition that mirrors `ProjectBudgetQuery`: for each selection it resolves the chosen bid's current priced BoQ, scales it by the project's apartment count (`EffectiveTotal` / `EffectiveTotalWithVat`), accumulates **per currency**, and adds an approximate EUR-equivalent via `IExchangeRateProvider`. A chosen bid with no current priced BoQ yields a line marked *not priced* that contributes nothing. It also serves the editor's **candidate** listing (the priced bids available per work package).

### HTTP API

- `GET    /api/projects/{projectId}/cost-scenarios` — list summaries.
- `POST   /api/projects/{projectId}/cost-scenarios` — create.
- `GET    /api/projects/{projectId}/cost-scenarios/candidates` — per-work-package candidate bids.
- `GET    /api/cost-scenarios/{id}` — the computed cost picture (breakdown + totals).
- `PUT    /api/cost-scenarios/{id}` — update name/description.
- `POST   /api/cost-scenarios/{id}/selections` — choose a bid for a work package (upsert).
- `DELETE /api/cost-scenarios/{id}/work-packages/{workPackageId}` — exclude a work package.
- `DELETE /api/cost-scenarios/{id}` — delete the scenario.

### MCP

`McpServer/Tools/CostScenarioTools.cs` exposes the same operations to an agent: `list_cost_scenarios`, `create_cost_scenario`, `get_cost_scenario`, `get_cost_scenario_candidates`, `include_bid_in_scenario`, `remove_work_package_from_scenario`, `delete_cost_scenario`.

### Frontend

Under `projects/[id]/cost-scenarios/` (beside `budget/`): a list page, a `new` form, the simulator detail page (a per-work-package auto-submitting bid picker + the breakdown and totals tables, mirroring the budget page), and an `edit` form. Reachable from a link on the project detail page. Romanian strings under `costScenario.*` in the i18n catalog.

### Persistence

EF migration `AddCostScenarios`: tables `cost_scenarios` and `cost_scenario_selections` (composite key `(CostScenarioId, WorkPackageId)`, cascade delete; `ProjectId` indexed).

## Open Questions

- Saved scenario-to-scenario comparison (a diff view) if visual comparison proves insufficient.
