# Order Service

ASP.NET Core 10 Web API, Clean Architecture, EF Core 10 + SQL Server. Owns checkout and order history. Validates JWTs issued by Identity. Checkout doesn't take a request body — it calls the Cart service over HTTP (forwarding the caller's bearer token) to read the current cart, snapshots it into an order, then calls Cart again to clear it. A second concrete example of service-to-service communication, and the synchronous version of the flow that milestone 4 will make event-driven via Azure Service Bus.

## Endpoints

All require `Authorization: Bearer <token>`.

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/orders` | Checkout — reads the current cart from Cart service, creates an order, clears the cart. 400 if the cart is empty. |
| GET | `/api/orders` | Current user's order history |
| GET | `/api/orders/{id}` | A single order — 404 if it doesn't belong to the caller |
| GET | `/swagger` | Swagger UI (Development only) |

## Running locally (without Docker)

Requires SQL Server at `localhost,1433` and the Cart API reachable at the URL in `CartApi:BaseUrl` (`appsettings.json`).

```bash
dotnet restore
dotnet ef migrations add InitialCreate --project Order.Infrastructure --startup-project Order.Api   # only if migrations don't exist yet
dotnet run --project Order.Api
```

## Running via Docker Compose

From the repo root:

```bash
docker-compose up --build db catalog-api cart-api order-api
```

Reachable at `http://localhost:5004` directly, or via the gateway at `http://localhost:5000/api/orders`.

## Tests

```bash
dotnet test
```

Covers `OrderService`: empty-cart checkout returns null, a populated cart becomes an order and clears the cart, and order lookups are scoped to the requesting user — with `IOrderRepository` and `ICartClient` mocked.
