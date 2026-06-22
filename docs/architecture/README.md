# Architecture

System architecture, diagrams, and technical decisions.

## Overview

Home Project Management is a multi-service application orchestrated with .NET Aspire.

```
┌─────────────┐      HTTP/REST      ┌──────────────┐      SQL      ┌────────────┐
│  Next.js    │ ──────────────────► │ .NET Core    │ ────────────► │ PostgreSQL │
│  Frontend   │ ◄────────────────── │ API Service  │ ◄──────────── │ Database   │
└─────────────┘                     └──────────────┘               └────────────┘
        ▲                                   ▲                             ▲
        └───────────────────────────────────┴─────────────────────────────┘
                          .NET Aspire AppHost (orchestration)
```

## Components

| Component | Technology | Project | Responsibility |
| --- | --- | --- | --- |
| Frontend | Next.js | — | User interface, client-side routing, calls the API |
| API | .NET Core | `HomeProjectManagement.ApiService` | Business logic, REST endpoints, data access |
| Database | PostgreSQL | — | Persistent storage |
| Orchestrator | .NET Aspire | `HomeProjectManagement.AppHost` | Wires up services, service discovery, local dev runtime |
| Shared defaults | .NET | `HomeProjectManagement.ServiceDefaults` | Telemetry, health checks, resilience, service discovery config |

## Orchestration

The **AppHost** is the entry point for local development. It provisions the
PostgreSQL container, starts the API service, and (where configured) the
Next.js frontend, wiring service discovery and connection strings between them.

## Domain Model

The DDD domain model — entities, relationships, ubiquitous language, and the
aggregate-root decisions — is documented in [`domain-model.md`](./domain-model.md).

## Hexagonal Architecture & Domain Mapping

How the domain model maps onto the DDD/Hexagonal code structure — projects, the
dependency rule, ports & adapters, and the per-aggregate layout — is in
[`hexagonal-architecture.md`](./hexagonal-architecture.md).

## Decision Records

Add Architecture Decision Records (ADRs) here as `NNNN-short-title.md`.
