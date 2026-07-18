# commerce-app-lab

A practice commerce platform built to gain hands-on experience with a specific stack: **.NET microservices, Angular, Azure (AKS/App Service), MCP, and applied AI/LLM**. Companion project to [dotnet-scale-lab](https://github.com/nedal-erekat/dotnet-scale-lab), which covers backend scalability patterns (caching, load balancing) that this project reuses but doesn't re-teach.

See [ROADMAP.md](ROADMAP.md) for the full milestone plan and target architecture.

## Tech stack

| Layer | Technology |
|-------|-----------|
| Backend services | ASP.NET Core 10, EF Core 10, Clean Architecture per service |
| Frontend | Angular 19 (storefront + admin apps) |
| Database | SQL Server (Azure SQL in the cloud) |
| Cache | Redis (Azure Cache for Redis in the cloud) |
| API Gateway | YARP |
| Messaging | Azure Service Bus |
| Cloud | Azure (AKS, ACR, Bicep IaC) |
| AI | MCP server (.NET, `ModelContextProtocol` SDK) + shopping assistant (MCP client + Anthropic Messages API agentic loop) |
| Containers | Docker Compose (local), AKS (cloud) |

## Project structure

```
commerce-app-lab/
├── ROADMAP.md
├── docker-compose.yml
├── services/            ← one ASP.NET Core microservice per folder
│   ├── Catalog/
│   ├── Identity/
│   ├── Cart/
│   ├── Order/
│   ├── OrderProcessing/ ← Worker Service, no HTTP — consumes order-placed events
│   ├── Gateway/
│   ├── Mcp/             ← MCP server exposing commerce tools over Streamable HTTP
│   └── Assistant/       ← AI shopping assistant — MCP client + agentic loop over Claude
├── frontend/            ← Angular workspace
│   └── projects/
│       ├── storefront/
│       └── admin/
├── infra/
│   ├── bicep/                 ← Azure IaC (AKS, ACR, SQL, Redis, Service Bus) + deploy/teardown scripts
│   ├── k8s/                   ← Kubernetes manifests for all 8 services
│   └── servicebus-emulator/   ← local Service Bus emulator config (queue definitions)
└── load-tests/                ← k6 scripts (see TESTING.md)
```

## Status

The roadmap (milestones 0–9) is complete. Milestone 9 added k6 load tests (`load-tests/`) and OpenTelemetry distributed tracing + metrics across all eight services — see [TESTING.md](TESTING.md) for exactly what ran live in this sandbox versus what's correct-by-inspection only (short version: Docker image pulls were blocked by this sandbox's network policy, so the k6 numbers there come from the real Gateway fronting a throwaway stand-in for Catalog, not the real SQL-Server-backed service — but the OpenTelemetry distributed-trace propagation itself was verified live and for real). Milestone 5's Azure deployment still hasn't been run against real Azure — see [infra/bicep/README.md](infra/bicep/README.md). See the [case study](#case-study) below for the project as a whole.

## Getting started

**Backend + infra (Docker Compose):**

```bash
export ANTHROPIC_API_KEY=sk-ant-...   # required — compose fails fast without it
docker-compose up --build
```

Everything goes through the gateway at `http://localhost:5000`. Individual services are still reachable directly for debugging/Swagger: Catalog `5001`, Identity `5002`, Cart `5003`, Order `5004`, MCP server `5005`, Assistant `5006` (Swagger at `/swagger` on the REST services, in Development). `order-processing-worker` has no HTTP endpoint — watch its logs to see it consuming `order-placed` messages.

**Frontend:**

```bash
cd frontend
npm install
npx ng serve storefront   # http://localhost:4200
npx ng serve admin        # http://localhost:4300
```

The chat widget appears bottom-right once you're logged in. Log into the admin app with the seeded admin account: `admin@commerce-app-lab.local` / `Admin123!`.

Each backend service documents its own run/migration instructions in its own README: [Catalog](services/Catalog/README.md), [Identity](services/Identity/README.md), [Cart](services/Cart/README.md), [Order](services/Order/README.md), [OrderProcessing](services/OrderProcessing/README.md), [Gateway](services/Gateway/README.md), [Mcp](services/Mcp/README.md), [Assistant](services/Assistant/README.md).

**Azure:**

```bash
RESOURCE_GROUP=commerce-app-lab-rg SQL_ADMIN_PASSWORD='...' ANTHROPIC_API_KEY='sk-ant-...' ./infra/bicep/deploy.sh
```

See [infra/bicep/README.md](infra/bicep/README.md) — spin up before a demo, tear down after with `./infra/bicep/teardown.sh`.

## Case study

**Why this exists:** a self-directed build to get hands-on with a specific stack — .NET microservices,
Angular, Azure/AKS, MCP, and applied AI/LLM — the same shape of stack named in a job posting that
prompted it. Nine milestones, each one a working, demoable increment rather than a big-bang build.
See [DESIGN-DECISIONS.md](DESIGN-DECISIONS.md) for the "why this, not the alternative" behind every
notable call below.

**What it demonstrates, milestone by milestone:**

- **Clean Architecture per service** (Catalog, Identity, Cart, Order) — each with its own EF Core
  `DbContext`, database, and Dockerfile. Genuinely independent deployables, not layered folders in one
  process.
- **A YARP API Gateway** as the single entry point for both Angular apps and the MCP server — no
  service ever gets called directly from the frontend.
- **Event-driven messaging**: `Order` publishes to Azure Service Bus, a separate `OrderProcessing`
  worker consumes it — decoupled, async inventory handling.
- **Cloud IaC**: Bicep for AKS/ACR/Azure SQL/Redis/Service Bus, plain Kubernetes manifests (no Helm),
  GitHub Actions CI/CD split into a safe always-on `ci.yml` and cost-incurring `workflow_dispatch`-only
  deploy/teardown workflows.
- **MCP, done for real**: a .NET MCP server (official SDK) exposing four commerce tools over Streamable
  HTTP, and a separate assistant service that's an MCP *client* — Claude decides which tool to call,
  the assistant executes it via MCP and loops until Claude has an answer, with the caller's JWT
  stripped from what the model sees and injected server-side only when a tool actually needs it.
- **Enterprise/portal development**: a second Angular app (the admin portal) with its own auth guard
  and role-gated (`[Authorize(Roles = "Admin")]`) backend endpoints, sharing the same JWT issued by
  Identity.
- **Observability and load testing**: OpenTelemetry distributed tracing + metrics across all eight
  services, and k6 load tests against the Gateway — see [TESTING.md](TESTING.md).

**What's honestly unverified:** the Azure deployment (Bicep/AKS) has never been run against a real
Azure subscription — no Azure CLI or credentials were available in any session this was built in. It's
reviewed by eye, not compiled or applied. Every other milestone has at least been built, unit-tested,
and exercised locally (Docker Compose where available, `dotnet run`/Playwright/k6 directly against a
single service otherwise) — each service's own README and [TESTING.md](TESTING.md) say exactly what
that exercising did and didn't cover, rather than claiming more than was actually checked.
