# Kubernetes manifests

Plain YAML (no Helm) — one Deployment + Service pair per HTTP service, applied by [`infra/bicep/deploy.sh`](../bicep/deploy.sh) after the Bicep deployment. Not meant to be applied by hand except for debugging; `deploy.sh` handles secret creation and the `${ACR_LOGIN_SERVER}` image substitution via `envsubst`.

| File | Deploys |
|------|---------|
| `configmap.yaml` | Shared non-secret config (Jwt issuer/audience, inter-service URLs, YARP cluster addresses) |
| `secret.example.yaml` | **Documentation only** — shows the `app-secrets` shape `deploy.sh` actually creates from Bicep outputs |
| `catalog.yaml`, `identity.yaml`, `cart.yaml`, `order.yaml` | Deployment + ClusterIP Service, each with `/health` liveness/readiness probes |
| `order-processing.yaml` | Deployment only — it's a worker, no HTTP endpoint, no Service |
| `gateway.yaml`, `mcp.yaml` | Deployment + a `LoadBalancer` Service each — the two resources here with an always-on Azure cost (a public IP apiece) even when pods are scaled down. `mcp.yaml` gets its own public IP so external MCP clients can reach it without going through the gateway's REST routes. |

All services request modest resources (50m CPU / 64–96Mi memory) to fit seven services plus AKS system pods on a single `Standard_B2s` node — this is a demo-sized deployment, not a production capacity plan.

Every ASP.NET Core service exposes `GET /health` (via `AddHealthChecks()`/`MapHealthChecks`, added specifically for these probes) — it wasn't needed before Docker Compose's health-check-free local setup.
