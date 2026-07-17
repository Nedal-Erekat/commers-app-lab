# commerce-app-lab — Claude Code Project Context

This file is auto-loaded by Claude Code at the start of every session.

---

## What this project is

A practice commerce platform built specifically to gain hands-on, interview-ready experience with: **.NET microservices, Angular, Azure (AKS/App Service), MCP, and applied AI/LLM**. Sibling project to [dotnet-scale-lab](https://github.com/nedal-erekat/dotnet-scale-lab) (backend scalability patterns — caching, load balancing). This project reuses those patterns per-service but focuses on the gaps ScaleLab doesn't cover: real microservices, Angular, cloud deployment, MCP, and AI.

**Source of truth for scope and sequencing:** [ROADMAP.md](ROADMAP.md). Always check it before starting work — build the current milestone, not ahead of it. Update its status column as milestones complete.

**Current milestone:** 0 (scaffold) — done. Next up: milestone 1 (Catalog microservice + Angular storefront).

---

## Tech stack

| Layer | Technology |
|-------|-----------|
| Backend services | ASP.NET Core 9, EF Core 9 — one Clean Architecture solution per service |
| Frontend | Angular 19 (standalone components), two apps in one workspace: `storefront`, `admin` |
| Database | SQL Server locally, Azure SQL (Basic tier) in the cloud |
| Cache | Redis locally, Azure Cache for Redis (Basic tier) in the cloud |
| API Gateway | YARP |
| Messaging | Azure Service Bus (order events) |
| Cloud | Azure — AKS (small node pool), ACR, Bicep IaC |
| AI | .NET MCP server exposing commerce tools; LLM-driven shopping assistant as an MCP client |
| Containers | Docker Compose (local dev), AKS (cloud) |

---

## Project structure

```
commerce-app-lab/
├── ROADMAP.md            ← milestone plan, target architecture — check first
├── docker-compose.yml     ← local infra + services, uncommented as milestones land
├── services/              ← one ASP.NET Core microservice per folder, each independently deployable
│   ├── Catalog/
│   ├── Identity/
│   ├── Cart/
│   ├── Order/
│   ├── Gateway/
│   └── Mcp/
├── frontend/               ← Angular workspace (npm install at this level)
│   └── projects/
│       ├── storefront/
│       └── admin/
└── infra/
    └── bicep/               ← Azure IaC + teardown script
```

Each service under `services/` gets its own `.sln`, its own EF Core `DbContext`/database, and its own Dockerfile — they are genuinely independent deployables, not layered folders sharing one process. Reuse ScaleLab's proven per-service conventions (file-scoped namespaces, async throughout, `.AsNoTracking()` on reads, Clean Architecture layering) rather than reinventing them.

---

## Behaviour guidelines for Claude

- Build the current ROADMAP.md milestone only — don't jump ahead to later milestones' features.
- Ask before implementing if a requirement is ambiguous or a milestone's scope seems wrong.
- Do not add error handling, abstractions, or features beyond what the current milestone needs.
- Do not write comments that describe what the code does — only write one when the *why* is non-obvious.
- Real Azure resources cost money: never leave AKS/Azure SQL/Redis running after a session unless explicitly asked to keep them up. Use the milestone 5 teardown script.
- Keep responses concise. No trailing summaries of what was just done unless asked.
- At the end of every change, provide a suggested commit message in this format:
  ```
  "<type>: <short description>"
  ```
