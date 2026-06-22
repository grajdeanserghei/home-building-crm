# Project Overview

- **Status:** Approved
- **Date:** 2026-06-22
- **Related:** [Architecture](./architecture/), [Specifications](./specifications/)

## Summary

Home Project Management is a private, internal tool for tracking the construction
of a **duplex** that the owners are building together with friends. It gives the
small group of stakeholders a shared place to follow progress and coordinate the
work involved in building their home.

## Who it is for

The application is used by **four people** — the owner, the owner's spouse, and
two friends — who are jointly building the duplex. It is an **internal tool only**;
it is not public and is not intended for a wider audience at this stage.

Because the group is small and fixed, the tool can be opinionated and tailored to
exactly how these four people want to track and manage the build, rather than
being a general-purpose product.

## Purpose and goals

- Give all four stakeholders a single, shared view of how the home build is
  progressing.
- Support the different ways each person wants to use the tool to manage and
  follow the project.
- Serve as the coordination point for the build while it is underway.

## Access and authentication

- The tool **requires authentication**: only the **four** named stakeholders may
  access it.
- There is no open sign-up. Access is restricted to this fixed group.

## Architecture mandate

The system must be built following:

- **Domain-Driven Design (DDD)** — the domain model and ubiquitous language drive
  the design.
- **Hexagonal architecture** (ports and adapters) — the domain core is isolated
  from infrastructure concerns (web, persistence, external services) behind
  explicit ports.

These are hard requirements that shape how the backend is structured. See the
[architecture documentation](./architecture/) for how the current implementation
maps onto these principles, and for decision records as the design evolves.

## Domain entities

The core domain entities will be described here as they are defined. _(To be
detailed.)_

## Non-goals (for now)

- Public access or multi-tenant use beyond the four stakeholders.
- Open self-service registration.
