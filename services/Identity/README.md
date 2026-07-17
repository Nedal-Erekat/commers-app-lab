# Identity Service

ASP.NET Core 10 Web API, ASP.NET Core Identity + EF Core 10 + SQL Server, JWT issuance. Owns users, roles (`Customer`, `Admin`), and auth for the storefront and admin portal. Other services validate the JWTs this service issues — they don't call back into Identity per-request.

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/auth/register` | `{ email, password }` → creates a `Customer` user, returns a JWT |
| POST | `/api/auth/login` | `{ email, password }` → returns a JWT |
| GET | `/api/auth/me` | Requires `Authorization: Bearer <token>`; returns the caller's email + roles |
| GET | `/swagger` | Swagger UI (Development only) — has a "Bearer" auth button for trying protected endpoints |

JWTs are signed HS256 with the `Jwt:Key` in `appsettings.json` — a dev-only secret committed for lab convenience. Replace it with an Azure Key Vault secret / env var before any real deployment (milestone 5).

## Running locally (without Docker)

Requires SQL Server reachable at `localhost,1433` (see `appsettings.json`).

```bash
dotnet restore
dotnet ef migrations add InitialCreate --project Identity.Infrastructure --startup-project Identity.Api   # only if migrations don't exist yet
dotnet run --project Identity.Api
```

Applies migrations and seeds the `Customer`/`Admin` roles on first run.

## Running via Docker Compose

From the repo root:

```bash
docker-compose up --build db identity-api
```

The API is reachable at `http://localhost:5002`.

## Tests

```bash
dotnet test
```

Covers `TokenService`'s JWT claim/issuer/audience/expiry generation. `AuthService` isn't unit tested directly — it's a thin orchestration layer over ASP.NET Core Identity's `UserManager`, which would need substantial mocking to test in isolation; its behavior is exercised via the endpoints instead.
