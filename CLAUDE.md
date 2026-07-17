# commerce-app-lab — Claude Code Project Context

This file is auto-loaded by Claude Code at the start of every session.

---

## What this project is

A practice commerce platform built specifically to gain hands-on, interview-ready experience with: **.NET microservices, Angular, Azure (AKS/App Service), MCP, and applied AI/LLM**. Sibling project to [dotnet-scale-lab](https://github.com/nedal-erekat/dotnet-scale-lab) (backend scalability patterns — caching, load balancing). This project reuses those patterns per-service but focuses on the gaps ScaleLab doesn't cover: real microservices, Angular, cloud deployment, MCP, and AI.

**Source of truth for scope and sequencing:** [ROADMAP.md](ROADMAP.md). Always check it before starting work — build the current milestone, not ahead of it. Update its status column as milestones complete.

**Current milestone:** 8 (Angular admin portal) — done. A second Angular app (`frontend/projects/admin`, served on port 4300) manages products (CRUD) and orders (view all, update status) across all customers, gated by an `Admin` role checked both client-side (route guard reads the JWT's role claim) and server-side (`[Authorize(Roles = "Admin")]` on new Catalog/Order endpoints). Identity seeds a dev admin account (`admin@commerce-app-lab.local` / `Admin123!`) on startup. Catalog previously had no JWT auth at all — that was added as part of this milestone, mirroring Order.Api's setup. Next up: milestone 9 (load testing, observability, teardown, write-up). Milestone 5's Azure IaC is still unverified against real Azure — see its note below.

Milestones 6 and 7 (MCP + AI) were built on `feature/ai-mcp` and merged into `main` via fast-forward. All work, including milestone 8, now happens directly on `main`.

The Angular storefront now calls everything through the Gateway (`http://localhost:5000`) instead of individual service ports — `environment.ts`/`environment.development.ts` expose a single `apiUrl`. Keep it that way; don't reintroduce per-service URLs in the frontend.

---

## Tech stack

| Layer | Technology |
|-------|-----------|
| Backend services | ASP.NET Core 10, EF Core 10 — one Clean Architecture solution per service |
| Frontend | Angular 19 (standalone components), two apps in one workspace: `storefront`, `admin` |
| Database | SQL Server locally, Azure SQL (Basic tier) in the cloud |
| Cache | Redis locally, Azure Cache for Redis (Basic tier) in the cloud |
| API Gateway | YARP |
| Messaging | Azure Service Bus (order events) |
| Cloud | Azure — AKS (small node pool), ACR, Bicep IaC |
| AI | .NET MCP server (`ModelContextProtocol`/`.AspNetCore` SDK v1.4.1, Streamable HTTP transport) exposing commerce tools; `services/Assistant` as the MCP client, agentic loop over the raw Anthropic Messages API (`System.Text.Json.Nodes`, no SDK — see its README) |
| Containers | Docker Compose (local dev), AKS (cloud) |

> Note: this project targets **.NET 10** (current LTS), not .NET 9 like dotnet-scale-lab — .NET 9 is STS and past its support window as of mid-2026. Reuse ScaleLab's patterns, not its exact package versions.

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
│   ├── OrderProcessing/   ← Worker Service (no HTTP), consumes Service Bus queue
│   ├── Gateway/
│   ├── Mcp/               ← MCP server (Mcp.Server) — calls the Gateway, same as the frontend
│   └── Assistant/         ← MCP client + agentic loop over Claude; calls Mcp.Server, not the Gateway
├── frontend/               ← Angular workspace (npm install at this level)
│   └── projects/
│       ├── storefront/
│       └── admin/
└── infra/
    ├── bicep/                 ← Azure IaC (main.bicep) + deploy.sh/teardown.sh
    ├── k8s/                   ← Kubernetes manifests, one Deployment(+Service) per HTTP service
    └── servicebus-emulator/   ← local emulator config.json (queue definitions)
```

Each service under `services/` gets its own `.sln`, its own EF Core `DbContext`/database, and its own Dockerfile — they are genuinely independent deployables, not layered folders sharing one process. Reuse ScaleLab's proven per-service conventions (file-scoped namespaces, async throughout, `.AsNoTracking()` on reads, Clean Architecture layering) rather than reinventing them.

Cross-service contracts (event payloads, HTTP client DTOs) are deliberately duplicated per-service rather than shared via a common library — each service owns its own copy of what it needs from another service's API. Keep doing this; don't introduce a shared contracts package.

The MCP server (`services/Mcp`) never calls Catalog/Cart/Order directly — it goes through the Gateway, exactly like the Angular apps. Its `get-order-status` and `add-to-cart` tools take a `bearerToken` as a plain string argument rather than the MCP server implementing OAuth itself.

The Assistant service (`services/Assistant`) is that MCP client. It strips `bearerToken` out of the tool schema it shows Claude and injects the caller's real JWT (from the `Authorization` header on `POST /api/chat`) only when actually invoking `get-order-status`/`add-to-cart` — the model never sees or reasons about the token, only which tool to call and with what business arguments. Conversation history is in-memory per `conversationId` (`InMemoryConversationStore`) — lost on restart, which is fine for a lab.

Locally, Azure Service Bus is the official [Service Bus emulator](https://learn.microsoft.com/azure/service-bus-messaging/overview-emulator) run via Docker Compose (`sb-emulator` + its `sqledge` metadata store), not a real Azure namespace.

Every ASP.NET Core service (not the OrderProcessing worker — it has no HTTP) exposes `GET /health` via `AddHealthChecks()`/`MapHealthChecks`, added in milestone 5 specifically for AKS liveness/readiness probes. Keep new services doing the same.

`.github/workflows/deploy-azure.yml` and `teardown-azure.yml` are `workflow_dispatch`-only (never on push) since they cost real money — don't change that trigger without being asked.

Milestone 5's Bicep (`infra/bicep/main.bicep`) has never been run against real Azure — no Azure CLI/credentials were available when it was written. It's reviewed-by-eye, not compiled. Before relying on it, run `az deployment group validate` and expect to fix schema drift, particularly the AKS `sku` block (flagged inline).

`docker-compose.yml` requires `ANTHROPIC_API_KEY` in the environment (`${ANTHROPIC_API_KEY:?...}`) — `docker-compose up` fails fast with a clear message if it's unset, rather than starting a broken assistant service.

To test a JWT-protected endpoint without a live Identity service/SQL Server (useful when SQL Server isn't available): mint an HS256 token by hand with the same dev signing key every service shares (`appsettings.json`'s `Jwt:Key`) — standard header/payload/HMAC-SHA256 base64url, no library needed for HS256. This is how milestone 7's Assistant service got exercised end-to-end without Identity running.

Role-based access follows the same shared-JWT model: `Identity.Api/Program.cs` seeds both roles (`Customer`, `Admin`) and a dev admin user at startup; `TokenService` puts every role the user has into `ClaimTypes.Role` claims on the JWT. Any downstream service just needs `[Authorize(Roles = "Admin")]` — no cross-service role lookup. The Angular admin app's route guard reads the same `roles` array off the login response (stored alongside the token) rather than decoding the JWT client-side.

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
