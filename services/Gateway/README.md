# API Gateway

YARP-based reverse proxy — the single entry point the Angular apps talk to. Routes by path prefix to the backend microservices; doesn't do any auth itself (each downstream service validates its own JWTs).

| Path prefix | Routes to |
|--------------|-----------|
| `/api/products/**` | Catalog service |
| `/api/auth/**` | Identity service |
| `/api/cart/**` | Cart service |
| `/api/orders/**` | Order service |

Configuration lives entirely in `appsettings.json` under `ReverseProxy` (Routes + Clusters) — no proxy code beyond `app.MapReverseProxy()` in `Program.cs`. `appsettings.json` points at `localhost` ports for local dev; Docker Compose overrides the cluster destination addresses via env vars to the Docker service names.

## Running locally (without Docker)

Requires Catalog, Identity, Cart, and Order all running locally first (see their own READMEs).

```bash
dotnet run --project Gateway.Api
```

Listens on `http://localhost:5000`.

## Running via Docker Compose

```bash
docker-compose up --build
```

The gateway is the only backend port the frontend should ever call: `http://localhost:5000`.
