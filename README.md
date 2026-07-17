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
| AI | MCP server (.NET) + LLM-driven shopping assistant |
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
│   └── Mcp/
├── frontend/            ← Angular workspace
│   └── projects/
│       ├── storefront/
│       └── admin/
└── infra/
    ├── bicep/                 ← Azure IaC
    └── servicebus-emulator/   ← local Service Bus emulator config (queue definitions)
```

## Status

Milestone 4 is done: checkout publishes an `OrderPlaced` message to an Azure Service Bus queue, consumed asynchronously by a new [OrderProcessing worker](services/OrderProcessing/README.md) that decrements product stock (via Catalog) and logs a simulated order-confirmation notification. Locally this runs against the official Service Bus emulator via Docker Compose rather than a real Azure namespace. See [ROADMAP.md](ROADMAP.md) for what's next.

## Getting started

**Backend + infra (Docker Compose):**

```bash
docker-compose up --build
```

Everything goes through the gateway at `http://localhost:5000`. Individual services are still reachable directly for debugging/Swagger: Catalog `5001`, Identity `5002`, Cart `5003`, Order `5004` (Swagger at `/swagger` on each, in Development). `order-processing-worker` has no HTTP endpoint — watch its logs to see it consuming `order-placed` messages.

**Frontend:**

```bash
cd frontend
npm install
npx ng serve storefront   # http://localhost:4200, or: npx ng serve admin
```

Each backend service documents its own run/migration instructions in its own README: [Catalog](services/Catalog/README.md), [Identity](services/Identity/README.md), [Cart](services/Cart/README.md), [Order](services/Order/README.md), [OrderProcessing](services/OrderProcessing/README.md), [Gateway](services/Gateway/README.md).
