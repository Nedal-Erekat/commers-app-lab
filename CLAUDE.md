# commerce-app-lab — Claude Code Project Context

This file is auto-loaded by Claude Code at the start of every session.

---

## What this project is

A practice commerce platform built specifically to gain hands-on, interview-ready experience with: **.NET microservices, Angular, Azure (AKS/App Service), MCP, and applied AI/LLM**. Sibling project to [dotnet-scale-lab](https://github.com/nedal-erekat/dotnet-scale-lab) (backend scalability patterns — caching, load balancing). This project reuses those patterns per-service but focuses on the gaps ScaleLab doesn't cover: real microservices, Angular, cloud deployment, MCP, and AI.

**Source of truth for scope and sequencing:** [ROADMAP.md](ROADMAP.md). Always check it before starting work — build the current milestone, not ahead of it. Update its status column as milestones complete.

**Current milestone:** 9 (load testing + observability) — done, last one on the roadmap. All eight services now register OpenTelemetry tracing (ASP.NET Core + HttpClient instrumentation) and metrics (+ runtime instrumentation), console-exported. k6 scripts live in `load-tests/`. See [TESTING.md](TESTING.md) for exactly what ran live this session vs. what's correct-by-inspection only — short version: this sandbox's Docker daemon could be started, but every image pull (MCR, Docker Hub, and the native `mssql-server` apt package) was blocked by network policy, so the k6 numbers came from the real Gateway fronting a throwaway local stand-in for Catalog, while OpenTelemetry's actual distributed-trace propagation was verified for real by running Gateway.Api standalone (it has no DB dependency) and confirming parent/child span correlation under concurrent load. Milestone 5's Azure IaC is still unverified against real Azure — see its note below.

Milestones 6 and 7 (MCP + AI) were built on `feature/ai-mcp` and merged into `main` via fast-forward. All work, including milestone 8, now happens directly on `main`.

The Angular storefront now calls everything through the Gateway (`http://localhost:5000`) instead of individual service ports — `environment.ts`/`environment.development.ts` expose a single `apiUrl`. Keep it that way; don't reintroduce per-service URLs in the frontend.

Post-roadmap addition: the storefront (not admin — no SEO reason to SSR an internal tool) now has Angular SSR (`ng add @angular/ssr`). Two things to know if you touch it: (1) any service reading browser-only APIs (`localStorage`, `window`, `document`) needs `isPlatformBrowser(inject(PLATFORM_ID))` guards — `AuthService` needed this fix, since the auth interceptor injects it on every HTTP call including the very first SSR render, and Node has no `localStorage`. (2) `app.routes.server.ts`'s per-route `RenderMode` only actually drives the *build-time* prerender step (confirmed: login/register get prerendered, others don't) — it does **not** get enforced at runtime by `server.ts`, because that requires `AngularNodeAppEngine`, which needs a build-manifest shape this Angular CLI version's scaffold (`CommonEngine`-based) doesn't produce; swapping it in throws "Angular app engine manifest is not set" at server startup (confirmed by trying it). Net effect: guarded routes (`account`/`cart`/`orders`) get SSR'd as their logged-out state (server can't see the browser's JWT) and self-correct via client hydration — a known, accepted trade-off of localStorage-based auth + SSR, not a bug.

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
├── DESIGN-DECISIONS.md   ← "why this, not the alternative" for every notable architectural call
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

`.github/workflows/deploy-azure.yml` and `teardown-azure.yml` are `workflow_dispatch`-only (never on push) since they cost real money — don't change that trigger without being asked. They share a `concurrency: azure-<resource_group>` group with no `cancel-in-progress` (queue, don't cancel — you never want a live Azure deploy/delete interrupted mid-flight).

Post-roadmap addition: `ci.yml` now runs both Angular apps' Karma unit tests too, not just `ng build` — which meant fixing both projects' default-scaffold `app.component.spec.ts` (still asserted the CLI template's `Hello, {title}` from before milestone 8 rewrote both `AppComponent`s). Both `karma.conf.js` files (generated via `ng generate config karma`) define a `ChromeHeadlessCI` launcher (`--no-sandbox --disable-gpu`) — needed for containers/CI runners that execute as root, harmless everywhere else. `.github/workflows/codeql.yml` builds every service by looping `services/*/*.slnx` (there's no root `.sln` tying the eight services together, so CodeQL's C# autobuild can't find one). `.github/dependabot.yml` covers nuget (root, recursive), npm (`/frontend`), one `docker` entry per service's Dockerfile (there are 8), and `github-actions` itself.

Milestone 5's Bicep (`infra/bicep/main.bicep`) has never been run against real Azure — no Azure CLI/credentials were available when it was written. It's reviewed-by-eye, not compiled. Before relying on it, run `az deployment group validate` and expect to fix schema drift, particularly the AKS `sku` block (flagged inline).

`docker-compose.yml` requires `ANTHROPIC_API_KEY` in the environment (`${ANTHROPIC_API_KEY:?...}`) — `docker-compose up` fails fast with a clear message if it's unset, rather than starting a broken assistant service.

To test a JWT-protected endpoint without a live Identity service/SQL Server (useful when SQL Server isn't available): mint an HS256 token by hand with the same dev signing key every service shares (`appsettings.json`'s `Jwt:Key`) — standard header/payload/HMAC-SHA256 base64url, no library needed for HS256. This is how milestone 7's Assistant service got exercised end-to-end without Identity running.

Role-based access follows the same shared-JWT model: `Identity.Api/Program.cs` seeds both roles (`Customer`, `Admin`) and a dev admin user at startup; `TokenService` puts every role the user has into `ClaimTypes.Role` claims on the JWT. Any downstream service just needs `[Authorize(Roles = "Admin")]` — no cross-service role lookup. The Angular admin app's route guard reads the same `roles` array off the login response (stored alongside the token) rather than decoding the JWT client-side.

If a sandbox's Docker daemon can be started but every image pull fails with a proxy 403, check `curl -sS $HTTPS_PROXY/__agentproxy/status` for `recentRelayFailures` before assuming Docker is unusable — it may be one specific blocked CDN host (`production.cloudfront.docker.com` blocked MCR/Docker Hub blobs and `pmc-geofence.trafficmanager.net` blocked the native `mssql-server` apt package in the session that built milestone 9), not a blanket policy. `proxy.golang.org` was allowlisted even when Docker Hub wasn't, so `go install go.k6.io/k6@latest` got a real k6 binary in that same session when neither `apt` nor GitHub releases worked.

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
