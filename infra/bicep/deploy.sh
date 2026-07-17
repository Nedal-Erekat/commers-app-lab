#!/usr/bin/env bash
# Deploys commerce-app-lab to Azure: Bicep infra, builds every service's image
# via ACR Tasks (no local Docker daemon needed), then applies the k8s manifests.
#
# NOT RUN in this session — no az CLI / Azure credentials in that sandbox. Review
# before running for real. Requires: az CLI (logged in), kubectl, envsubst.
#
# Usage:
#   RESOURCE_GROUP=commerce-app-lab-rg SQL_ADMIN_PASSWORD='...' ./deploy.sh
#
# Optional env vars: LOCATION (default eastus), NAME_PREFIX (default commerceapplab),
# JWT_KEY (generated with openssl if not supplied).

set -euo pipefail

RESOURCE_GROUP="${RESOURCE_GROUP:?Set RESOURCE_GROUP}"
LOCATION="${LOCATION:-eastus}"
NAME_PREFIX="${NAME_PREFIX:-commerceapplab}"
SQL_ADMIN_LOGIN="${SQL_ADMIN_LOGIN:-commerceapplabadmin}"
SQL_ADMIN_PASSWORD="${SQL_ADMIN_PASSWORD:?Set SQL_ADMIN_PASSWORD (Azure SQL password complexity rules apply)}"
JWT_KEY="${JWT_KEY:-$(openssl rand -base64 48)}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

echo "==> Resource group"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none

echo "==> Deploying Bicep (this provisions ACR, AKS, SQL, Redis, Service Bus)"
DEPLOY_OUTPUT=$(az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "$SCRIPT_DIR/main.bicep" \
  --parameters "$SCRIPT_DIR/main.parameters.json" \
  --parameters namePrefix="$NAME_PREFIX" location="$LOCATION" sqlAdminLogin="$SQL_ADMIN_LOGIN" \
  --parameters sqlAdminPassword="$SQL_ADMIN_PASSWORD" jwtKey="$JWT_KEY" \
  --query properties.outputs -o json)

ACR_LOGIN_SERVER=$(echo "$DEPLOY_OUTPUT" | jq -r .acrLoginServer.value)
AKS_NAME=$(echo "$DEPLOY_OUTPUT" | jq -r .aksName.value)
SQL_FQDN=$(echo "$DEPLOY_OUTPUT" | jq -r .sqlServerFqdn.value)
REDIS_HOST=$(echo "$DEPLOY_OUTPUT" | jq -r .redisHostName.value)
SB_NAMESPACE_FQDN=$(echo "$DEPLOY_OUTPUT" | jq -r .serviceBusNamespaceFqdn.value)
SB_AUTH_RULE_ID=$(echo "$DEPLOY_OUTPUT" | jq -r .serviceBusAuthRuleId.value)

echo "==> Fetching Redis and Service Bus keys"
REDIS_NAME=$(echo "$REDIS_HOST" | cut -d. -f1)
REDIS_KEY=$(az redis list-keys --name "$REDIS_NAME" --resource-group "$RESOURCE_GROUP" --query primaryKey -o tsv)
SB_KEY=$(az servicebus namespace authorization-rule keys list --ids "$SB_AUTH_RULE_ID" --query primaryKey -o tsv)

echo "==> Building and pushing images via ACR Tasks (no local Docker needed)"
for svc in Catalog:catalog-api Identity:identity-api Cart:cart-api Order:order-api OrderProcessing:order-processing-worker Gateway:gateway Mcp:mcp-server; do
  DIR="${svc%%:*}"
  IMAGE="${svc##*:}"
  echo "  -- $IMAGE"
  az acr build --registry "$(echo "$ACR_LOGIN_SERVER" | cut -d. -f1)" \
    --image "$IMAGE:latest" \
    "$REPO_ROOT/services/$DIR" \
    --output none
done

echo "==> Fetching AKS credentials"
az aks get-credentials --resource-group "$RESOURCE_GROUP" --name "$AKS_NAME" --overwrite-existing

echo "==> Creating k8s secrets"
kubectl create secret generic app-secrets \
  --from-literal=CatalogDbConnectionString="Server=tcp:${SQL_FQDN},1433;Database=CatalogDb;User Id=${SQL_ADMIN_LOGIN};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;" \
  --from-literal=IdentityDbConnectionString="Server=tcp:${SQL_FQDN},1433;Database=IdentityDb;User Id=${SQL_ADMIN_LOGIN};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;" \
  --from-literal=CartDbConnectionString="Server=tcp:${SQL_FQDN},1433;Database=CartDb;User Id=${SQL_ADMIN_LOGIN};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;" \
  --from-literal=OrderDbConnectionString="Server=tcp:${SQL_FQDN},1433;Database=OrderDb;User Id=${SQL_ADMIN_LOGIN};Password=${SQL_ADMIN_PASSWORD};Encrypt=True;TrustServerCertificate=False;" \
  --from-literal=RedisConnectionString="${REDIS_HOST}:6380,password=${REDIS_KEY},ssl=True,abortConnect=False" \
  --from-literal=JwtKey="${JWT_KEY}" \
  --from-literal=ServiceBusConnectionString="Endpoint=sb://${SB_NAMESPACE_FQDN}/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=${SB_KEY}" \
  --dry-run=client -o yaml | kubectl apply -f -

echo "==> Applying manifests"
kubectl apply -f "$REPO_ROOT/infra/k8s/configmap.yaml"
export ACR_LOGIN_SERVER
for manifest in catalog identity cart order order-processing gateway mcp; do
  envsubst '${ACR_LOGIN_SERVER}' < "$REPO_ROOT/infra/k8s/$manifest.yaml" | kubectl apply -f -
done

echo "==> Waiting for the gateway's public IP (this can take a few minutes)"
kubectl get service gateway --watch
