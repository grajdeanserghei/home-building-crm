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
| [Central Package Management (NuGet)](./guides/central-package-management.md) | Why and how all NuGet versions are pinned centrally in the root `Directory.Packages.props`. |
| [Building & publishing container images](./guides/container-images.md) | Dockerfiles and the image build/push flow for deploying to the homelab k3s cluster (the app-repo half of deployment). |

## Specifications (`specifications/`)

| Document | Status | What it covers |
| --- | --- | --- |
| [Specifications overview](./specifications/README.md) | — | Landing page for feature specs; points to the spec template. |
| [`_template.md`](./specifications/_template.md) | — | Starting template for new specifications. |
| [Project Budget View](./specifications/project-budget-view.md) | Implemented | Read-only `/projects/{id}/budget` page rolling up committed (contract) and candidate (BoQ) costs per currency. |
| [Romanian Localization](./specifications/romanian-localization.md) | Implemented | Romanian-only presentation layer plus the reviewed glossary that fixes the Romanian ubiquitous language. |
| [Remote MCP Server](./specifications/remote-mcp-server.md) | Implemented | A remote MCP server letting an AI agent drive conversational and document-based data entry; covers the prerequisite `Bid` and `BillOfQuantities` aggregates. |

## Conventions

- One document per topic; kebab-case filenames (e.g. `task-scheduling.md`).
- Start each spec with a short summary, status, and date (see `_template.md`).
- Link related documents to keep navigation easy.
- When you add or remove a document, update this index.
