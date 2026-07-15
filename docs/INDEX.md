# Documentation Index

A complete map of the documentation under `docs/`. Start with the
[Project Overview](./project-overview.md) for what this tool is and who it is for.

## Overview

| Document | What it covers |
| --- | --- |
| [Project Overview](./project-overview.md) | What the tool is, the four stakeholders it serves, access/auth requirements, and the DDD + Hexagonal architecture mandate. |
| [`README.md`](./README.md) | Top-level docs landing page: tech stack, folder structure, and conventions. |

## Architecture (`architecture/`)

| Document | What it covers |
| --- | --- |
| [Architecture overview](./architecture/README.md) | System diagram, components, Aspire orchestration, and where ADRs live. |
| [Domain Model](./architecture/domain-model.md) | DDD domain model — aggregates, relationships, aggregate boundaries, attributes, and the ubiquitous language. |
| [Hexagonal Architecture & Domain Mapping](./architecture/hexagonal-architecture.md) | How the domain model maps onto the ports-and-adapters code structure: projects, the dependency rule, per-aggregate layout. |

## Guides (`guides/`)

| Document | What it covers |
| --- | --- |
| [Guides overview](./guides/README.md) | Index of how-to guides and suggested topics. |
| [UI Principles — Read-First](./guides/ui-principles.md) | The frontend's read-first discipline: pages default to reading, creation is a separate `/new` destination, one primary action per view; `projects/page.tsx` as the reference and the forms-above-the-list pages to migrate. |
| [Central Package Management (NuGet)](./guides/central-package-management.md) | Why and how all NuGet versions are pinned centrally in the root `Directory.Packages.props`. |
| [Building & publishing container images](./guides/container-images.md) | Dockerfiles and the image build/push flow for deploying to the homelab k3s cluster (the app-repo half of deployment). |

## Specifications (`specifications/`)

| Document | Status | What it covers |
| --- | --- | --- |
| [Specifications overview](./specifications/README.md) | — | Landing page for feature specs; points to the spec template. |
| [`_template.md`](./specifications/_template.md) | — | Starting template for new specifications. |
| [Project Budget View](./specifications/project-budget-view.md) | Implemented | Read-only `/projects/{id}/budget` page rolling up committed (contract) and candidate (BoQ) costs per currency. |
| [Cost Scenarios (Cost Simulator)](./specifications/cost-scenarios.md) | Implemented | Saved "what-if" cost combinations — one chosen bid (its BoQ) per work package — with a computed per-currency total and EUR-equivalent; HTTP + MCP + `/projects/{id}/cost-scenarios` UI. |
| [Romanian Localization](./specifications/romanian-localization.md) | Implemented | Romanian-only presentation layer plus the reviewed glossary that fixes the Romanian ubiquitous language. |
| [Remote MCP Server](./specifications/remote-mcp-server.md) | Implemented | A remote MCP server letting an AI agent drive conversational and document-based data entry; covers the prerequisite `Bid` and `BillOfQuantities` aggregates. |
| [BoQ Excel Export](./specifications/boq-excel-export.md) | Implemented | Export a *deviz* to `.xlsx` — one worksheet per Section, Subsections as visually separated bands, summary sheet, live `SUM()` totals; ClosedXML behind a driven port. |
| [BoQ Line-Item Reordering](./specifications/boq-line-item-reordering.md) | Implemented | Drag-and-drop reorder of BoQ line items, including moving a line between subcapitols anywhere in the BoQ; root `MoveLineItem` with dense renumbering, a `move-line-item` endpoint, and a read-first "Arrange" mode using `@dnd-kit`. |
| [Construction Valuation](./specifications/construction-valuation.md) | Draft | The bank appraiser's *fișă de calcul a valorii construcției*: a per-project `ValuationCatalog` (itemized estimate + BoQ-section mapping) and dated `ConstructionValuation` snapshots (frozen completion assessments); enables estimate-vs-real-BoQ comparison and progress tracking. |
| [Valuation Comparison Basis](./specifications/valuation-comparison-basis.md) | Draft | Treats competing BoQs as alternatives, not additive: the estimate-vs-real read model selects one BoQ per work package via a `ComparisonBasis` — `Decided` (accepted-then-selected) by default, or `Scenario` for the cost simulator's what-if; adds `WorkPackageId` to `ValuationItemLink`. |

## Conventions

- One document per topic; kebab-case filenames (e.g. `task-scheduling.md`).
- Start each spec with a short summary, status, and date (see `_template.md`).
- Link related documents to keep navigation easy.
- When you add or remove a document, update this index.
