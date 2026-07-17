# Infrastructure (Bicep) — milestone 5

Provisions the Azure footprint: AKS (single small node pool, free control plane), Azure Container Registry, Azure SQL (one logical server, four Basic databases — one per service), Azure Cache for Redis (Basic), and Azure Service Bus (Basic + the `order-placed` queue). Everything else — the actual app deployments — lives in [`infra/k8s/`](../k8s/).

> **Not validated in this session.** The sandbox this was built in has no network path to install the Azure CLI or the standalone Bicep CLI (outbound requests to `dot.net`, GitHub release downloads, etc. are blocked by this environment's proxy), so `main.bicep` has only been reviewed by eye against the ARM schemas — never run through `bicep build` or `az deployment group validate`. Run validation before `create` the first time you deploy for real. The field most likely to have drifted from the current schema is the AKS `sku` block (`main.bicep`, flagged inline).

## What it provisions

| Resource | SKU | Why |
|----------|-----|-----|
| AKS | 1× `Standard_B2s` node, free control plane tier | Hosts all 6 services |
| Container Registry | Basic | Images built via `az acr build` (no local Docker needed) |
| Azure SQL (logical server) | 4× Basic database | One DB per service: CatalogDb, IdentityDb, CartDb, OrderDb |
| Azure Cache for Redis | Basic C0 | Catalog's cache-aside |
| Azure Service Bus | Basic + `order-placed` queue | Order → OrderProcessing |

Rough always-on cost if left running: AKS node ~$15/mo, 4× SQL Basic ~$20/mo, Redis Basic C0 ~$16/mo, Service Bus Basic ~$0.05/mo, ACR Basic ~$5/mo, plus the Gateway's LoadBalancer public IP (~$4/mo) once `infra/k8s/gateway.yaml` is applied. This is why the deploy/teardown scripts exist — spin up for a demo, tear down after, not left running.

## Deploying

Requires: `az` CLI (logged in — `az login`), `kubectl`, `jq`, `envsubst` (from `gettext`), `openssl`.

```bash
export RESOURCE_GROUP=commerce-app-lab-rg
export SQL_ADMIN_PASSWORD='choose one meeting Azure SQL complexity rules'
./infra/bicep/deploy.sh
```

This: creates the resource group, deploys `main.bicep`, builds and pushes all 6 images via `az acr build` (server-side — no local Docker daemon required), fetches AKS credentials, creates the `app-secrets` k8s Secret from the deployment outputs, and applies every manifest in `infra/k8s/`.

Or trigger it from GitHub Actions: **Actions → Deploy to Azure → Run workflow** (needs the `AZURE_CREDENTIALS`, `SQL_ADMIN_PASSWORD`, and `JWT_KEY` repo secrets — see [`.github/workflows/deploy-azure.yml`](../../.github/workflows/deploy-azure.yml)).

## Tearing down

```bash
RESOURCE_GROUP=commerce-app-lab-rg ./infra/bicep/teardown.sh
```

Or **Actions → Teardown Azure → Run workflow**. Deletes the whole resource group — everything Bicep created — in one shot.

## Files

| File | Purpose |
|------|---------|
| `main.bicep` | All Azure resources |
| `main.parameters.json` | Non-secret parameter defaults (region, name prefix) |
| `deploy.sh` / `teardown.sh` | The up/down flow described above |
