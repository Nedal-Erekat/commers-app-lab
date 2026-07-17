# Cart Service

ASP.NET Core 10 Web API, Clean Architecture, EF Core 10 + SQL Server. Owns the per-user shopping cart. Validates JWTs issued by Identity (doesn't issue its own). Calls the Catalog service over HTTP to resolve canonical product name/price when an item is added — the concrete example of service-to-service communication in this milestone.

## Endpoints

All require `Authorization: Bearer <token>`.

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/cart` | Current user's cart + total |
| POST | `/api/cart/items` | `{ productId, quantity }` → looks up the product via Catalog, adds/increments the line item |
| DELETE | `/api/cart/items/{productId}` | Remove a line item |
| DELETE | `/api/cart` | Clear the cart — also called by the Order service during checkout |
| GET | `/swagger` | Swagger UI (Development only) |

## Running locally (without Docker)

Requires SQL Server at `localhost,1433` and the Catalog API reachable at the URL in `CatalogApi:BaseUrl` (`appsettings.json`).

```bash
dotnet restore
dotnet ef migrations add InitialCreate --project Cart.Infrastructure --startup-project Cart.Api   # only if migrations don't exist yet
dotnet run --project Cart.Api
```

## Running via Docker Compose

From the repo root:

```bash
docker-compose up --build db catalog-api cart-api
```

Reachable at `http://localhost:5003` directly, or via the gateway at `http://localhost:5000/api/cart`.

## Tests

```bash
dotnet test
```

Covers `CartService`: adding a new item, incrementing an existing line item's quantity, removing an item, and the empty-cart case — with `ICartRepository` and `IProductCatalogClient` mocked.
