# MCP Server

A .NET MCP server (official [`ModelContextProtocol`](https://www.nuget.org/packages/ModelContextProtocol) / `ModelContextProtocol.AspNetCore` SDK, v1.4.1) exposing four commerce tools over the Streamable HTTP transport. It doesn't talk to the other services directly — every tool call goes through the [Gateway](../Gateway/README.md), same as the Angular storefront.

## Tools

| Tool | Auth | Description |
|------|------|-------------|
| `search-products` | none | Search the catalog by name prefix |
| `recommend-products` | none | Up to 20 products, optionally filtered by exact category |
| `get-order-status` | JWT | Look up an order's status and contents |
| `add-to-cart` | JWT | Add a product to a customer's cart |

**Simplification, stated plainly:** `get-order-status` and `add-to-cart` take a `bearerToken` as a plain tool argument rather than the MCP server implementing its own OAuth authorization flow (the MCP spec supports one; this doesn't). A caller has to obtain a JWT from `POST /api/auth/login` some other way and pass it into the tool call. That's a reasonable simplification for a lab, not something you'd want in a real multi-tenant deployment.

## Architecture

```
MCP client (Claude, etc.) ──MCP/HTTP──► Mcp.Server ──HTTP──► Gateway ──► Catalog / Cart / Order
```

`CommerceTools` ([McpServerToolType]) is a thin wrapper — it just serializes results to JSON. All validation (blank query, quantity < 1, missing bearer token, clamping `take` to 1–20) lives in `CommerceService`, which is what's unit tested. `GatewayCommerceClient` does the actual HTTP calls and is swapped for a mock in tests.

## Verified in this session

Unlike the Azure-dependent milestones, this one could actually be run: `dotnet run --project Mcp.Server`, then a real MCP JSON-RPC handshake over `curl` — `initialize`, `notifications/initialized`, `tools/list` (all four tools came back with the right names/descriptions/parameter schemas), and a `tools/call` against `search-products` (failed gracefully with `isError: true` since the Gateway wasn't running — no crash). This is the first milestone where the actual protocol surface, not just the C# unit tests, got exercised.

## Running locally (without Docker)

Requires the Gateway (and everything behind it) reachable at the URL in `appsettings.json`.

```bash
dotnet restore
dotnet run --project Mcp.Server
```

Listens on whatever `ASPNETCORE_URLS`/launch profile gives it; point an MCP client's Streamable HTTP transport at that URL.

## Running via Docker Compose

```bash
docker-compose up --build
```

Reachable at `http://localhost:5005`.

## Tests

```bash
dotnet test
```

Covers `CommerceService`'s validation and delegation to `ICommerceClient` (mocked) — trimming search queries, clamping `recommend-products`' `take`, rejecting blank bearer tokens and non-positive quantities.
