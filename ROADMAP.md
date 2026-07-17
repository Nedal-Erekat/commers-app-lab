# Roadmap

Built one milestone at a time, same style as [dotnet-scale-lab](https://github.com/nedal-erekat/dotnet-scale-lab): each row below is a working, demoable state before moving to the next.

**Why this project exists:** to build hands-on, interview-ready experience with a specific stack — .NET microservices, Angular, Azure (AKS/App Service), MCP, and applied AI/LLM — on a real e-commerce domain (catalog → cart → checkout → orders).

## Target architecture

```
Angular Storefront ──┐
Angular Admin Portal ─┼──► API Gateway (YARP) ──► Catalog Service   (ASP.NET Core, Clean Architecture)
MCP Server (.NET) ────┘                        ├─► Identity Service  (ASP.NET Core Identity + JWT)
        ▲                                       ├─► Cart Service
        │                                       └─► Order Service ──► Azure Service Bus ──► OrderProcessing worker
        │ tools
AI Assistant backend (MCP client) ── calls an LLM for reasoning
```

The MCP server is just another Gateway client — same REST contract the Angular apps use, no direct service-to-service shortcuts.

Each service owns its own EF Core context, its own database (or schema), and its own Dockerfile — genuinely independent deployables, not just layered folders in one process.

## Milestones

| # | Milestone | Status | Proves |
|---|-----------|--------|--------|
| 0 | Scaffold: repo structure, docker-compose skeleton, Angular workspace (storefront + admin apps) | ✅ done | Repo baseline |
| 1 | Catalog microservice + Angular storefront (browse/search) | ✅ done | .NET, Angular, REST |
| 2 | Identity microservice (JWT auth) + Angular login/register, route guards | ✅ done | Enterprise auth |
| 3 | Cart + Order microservices behind a YARP API Gateway, service-to-service calls | ✅ done | True microservices, REST |
| 4 | Order → Azure Service Bus → async inventory/notification handler | ✅ done | Event-driven/distributed patterns |
| 5 | Containerize all services, deploy to Azure: AKS (small node pool) + Azure SQL (Basic) + Azure Cache for Redis (Basic) + ACR, IaC via Bicep, GitHub Actions CI/CD | ✅ written, unverified | Azure/AKS |
| 6 | .NET MCP server exposing tools: `search-products`, `get-order-status`, `recommend-products`, `add-to-cart`; hosted on Azure | ✅ built + verified locally, not yet hosted on Azure | MCP via .NET & Azure |
| 7 | AI shopping assistant: chat widget in Angular → assistant backend as an MCP client → calls an LLM to decide which MCP tool to invoke (agentic loop) | ✅ built + verified as far as sandbox allows, no real Claude reply yet | Hands-on AI/LLM |
| 8 | Admin portal features in Angular (manage products/orders, role-based views) | next up | Enterprise/portal application development |
| 9 | Load testing (k6), observability, Azure teardown script, README + case-study write-up | planned | Interview-ready deliverable |

## Azure cost control

Real Azure, minimal footprint: a single small AKS node pool (control plane is free), Basic-tier SQL/Redis, Basic-tier Service Bus, everything defined in Bicep so `az deployment` up/down is one command. Spin up before a demo, tear down after — the teardown script ships alongside the IaC in milestone 5. Locally (and in this repo's CI, if any), messaging runs against the [Service Bus emulator](https://learn.microsoft.com/azure/service-bus-messaging/overview-emulator) via Docker Compose instead of a real namespace — milestone 5 is what actually points the connection string at Azure.
