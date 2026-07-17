# Order Processing Worker

A .NET Worker Service (no HTTP endpoints) that consumes the `order-placed` Azure Service Bus queue that the Order service publishes to on checkout. For each item on the order it calls Catalog's `POST /api/products/{id}/decrement-stock`, then logs a simulated order-confirmation notification. This is the async, event-driven counterpart to the synchronous Order → Cart calls from milestone 3.

## Why a separate service

Decoupling this from Order itself means a slow or temporarily-down inventory/notification step can't block checkout, and it can be scaled or redeployed independently. Order only has to get the message onto the queue — it doesn't wait for (or know about) what happens downstream.

## Failure handling

If decrementing stock for one item fails (insufficient stock, product gone), the handler logs a warning and keeps processing the rest of the order's items — a business-rule failure like "not enough stock" isn't something that retrying the same message would fix, so retrying the whole message would just dead-letter it for no benefit. See `OrderPlacedHandler` for the reasoning inline.

## Configuration

| Setting | Purpose |
|---------|---------|
| `ServiceBus:ConnectionString` | Service Bus (or emulator) connection string |
| `ServiceBus:QueueName` | Queue to consume — `order-placed` |
| `CatalogApi:BaseUrl` | Where to call `decrement-stock` |

## Running locally (without Docker)

Requires a Service Bus emulator (or real namespace) and the Catalog API both reachable at the URLs in `appsettings.json`.

```bash
dotnet restore
dotnet run --project OrderProcessing.Worker
```

## Running via Docker Compose

From the repo root — this brings up the Service Bus emulator (`sb-emulator` + its `sqledge` metadata store, defined by `infra/servicebus-emulator/config.json`) alongside the worker:

```bash
docker-compose up --build db catalog-api sqledge sb-emulator order-processing-worker order-api cart-api
```

> **Not runtime-verified in this session** — the sandbox this was built in has no Docker daemon, so the emulator wiring (image names, ports, connection string format) is based on Microsoft's published emulator docs but hasn't actually been run end-to-end. First thing to check when you run it for real.

## Tests

```bash
dotnet test
```

Covers `OrderPlacedHandler`: decrements stock for every item, keeps processing the rest of the order if one item's decrement fails, and does nothing for an empty item list — with `IInventoryClient` mocked. The Service Bus plumbing itself (`Worker.cs`) isn't unit tested since it needs a live broker; only the message-handling logic is.
