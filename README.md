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
│   ├── Gateway/
│   └── Mcp/
├── frontend/            ← Angular workspace
│   └── projects/
│       ├── storefront/
│       └── admin/
└── infra/
    └── bicep/           ← Azure IaC
```

## Status

Milestone 1 is done: the Catalog microservice (ASP.NET Core 10, EF Core 10, Redis cache-aside — see [services/Catalog/README.md](services/Catalog/README.md)) and the Angular storefront's product browsing/search page are both working. See [ROADMAP.md](ROADMAP.md) for what's next.

## Getting started

**Backend + infra (Docker Compose):**

```bash
docker-compose up --build db redis catalog-api
```

The Catalog API is reachable at `http://localhost:5001` (Swagger at `/swagger` in Development).

**Frontend:**

```bash
cd frontend
npm install
npx ng serve storefront   # http://localhost:4200, or: npx ng serve admin
```

Each backend service documents its own run/migration instructions in its own README as it lands (see [services/Catalog/README.md](services/Catalog/README.md) for the first one).
