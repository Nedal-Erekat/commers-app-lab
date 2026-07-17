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
└── infra/
    ├── bicep/                 ← Azure IaC (AKS, ACR, SQL, Redis, Service Bus) + deploy/teardown scripts
    ├── k8s/                   ← Kubernetes manifests for all 8 services
    └── servicebus-emulator/   ← local Service Bus emulator config (queue definitions)
```

## Status

Milestone 7 is done: an AI shopping assistant ([services/Assistant](services/Assistant/README.md)) — an MCP *client* that calls the milestone 6 MCP server, lets Claude decide which tool to invoke for a given message, executes it, and loops until Claude has a final answer. A floating chat widget in the Angular storefront talks to it. Verified as far as this sandbox allows: ran the assistant and MCP server together locally with a real (locally-minted) JWT and confirmed the whole pipeline — auth, tool discovery, request building — reaches the real Anthropic API and gets a well-formed response back, failing only on the missing API key. See [services/Assistant/README.md](services/Assistant/README.md) for exactly what was and wasn't tested, and [ROADMAP.md](ROADMAP.md) for what's next. (Milestone 5's Azure deployment still hasn't been run against real Azure — see [infra/bicep/README.md](infra/bicep/README.md).)

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
npx ng serve storefront   # http://localhost:4200, or: npx ng serve admin
```

The chat widget appears bottom-right once you're logged in.

Each backend service documents its own run/migration instructions in its own README: [Catalog](services/Catalog/README.md), [Identity](services/Identity/README.md), [Cart](services/Cart/README.md), [Order](services/Order/README.md), [OrderProcessing](services/OrderProcessing/README.md), [Gateway](services/Gateway/README.md), [Mcp](services/Mcp/README.md), [Assistant](services/Assistant/README.md).

**Azure:**

```bash
RESOURCE_GROUP=commerce-app-lab-rg SQL_ADMIN_PASSWORD='...' ANTHROPIC_API_KEY='sk-ant-...' ./infra/bicep/deploy.sh
```

See [infra/bicep/README.md](infra/bicep/README.md) — spin up before a demo, tear down after with `./infra/bicep/teardown.sh`.
