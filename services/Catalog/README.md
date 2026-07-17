# Catalog Service

ASP.NET Core 10 Web API, Clean Architecture (Domain → Application → Infrastructure → Api), EF Core 10, SQL Server, Redis cache-aside. Owns product data and search. Ported from the patterns proven in [dotnet-scale-lab](https://github.com/nedal-erekat/dotnet-scale-lab).

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/products?page=&pageSize=` | Paginated product list (cache-aside via Redis) |
| GET | `/api/products/search?q=` | Prefix search by name (bypasses cache) |
| GET | `/api/products/{id}` | Single product, cache-aside via Redis — used by the Cart service |
| POST | `/api/products/{id}/decrement-stock` | `{ quantity }` → decrements stock; 400 if insufficient or not found. Invalidates that product's cache entry. Called by the OrderProcessing worker after checkout. |
| GET | `/swagger` | Swagger UI (Development only) |

**Known staleness:** decrementing stock invalidates the single-product cache entry (`product_{id}`) but not the paginated list cache (`products_page_*`), so a product's stock shown via `GET /api/products` can lag up to 5 minutes behind reality after an order. `GET /api/products/{id}` is always fresh.

## Running locally (without Docker)

Requires SQL Server and Redis reachable at the addresses in `appsettings.json` (`localhost,1433` / `localhost:6379`).

```bash
dotnet restore
dotnet ef migrations add InitialCreate --project Catalog.Infrastructure --startup-project Catalog.Api   # only if migrations don't exist yet
dotnet run --project Catalog.Api
```

On first run it applies migrations and seeds 20,000 fake products (Bogus) if the `Products` table is empty.

## Running via Docker Compose

From the repo root:

```bash
docker-compose up --build db redis catalog-api
```

The API is reachable at `http://localhost:5001`.

## Tests

```bash
dotnet test
```

Covers `ProductService` pagination math and `CachedProductRepository`'s cache-aside behavior with an in-memory `IDistributedCache`.
