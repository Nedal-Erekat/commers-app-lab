// Azure footprint for commerce-app-lab (milestone 5).
//
// NOT VALIDATED against the Bicep compiler — this sandbox has no network path to
// install az CLI / the standalone Bicep CLI, so this has only been reviewed by eye
// against the ARM schemas. Run `az deployment group validate` before `create`.
// See infra/bicep/README.md for the deploy/teardown flow.

@description('Short lowercase-alphanumeric prefix used to build resource names (no hyphens — must be valid in ACR/Redis/SQL names too).')
@minLength(3)
@maxLength(12)
param namePrefix string = 'commerceapplab'

@description('Azure region — pick a cheap one (eastus, westus2, etc).')
param location string = resourceGroup().location

@description('Admin login for the Azure SQL logical server.')
param sqlAdminLogin string = 'commerceapplabadmin'

@secure()
@description('Admin password for the Azure SQL logical server.')
param sqlAdminPassword string

@secure()
@description('HS256 signing key shared by Identity (issues tokens) and Cart/Order (validate them). Generate a fresh one for this deployment — do not reuse the local dev key.')
param jwtKey string

var uniqueSuffix = uniqueString(resourceGroup().id)
var acrName = '${namePrefix}acr${uniqueSuffix}'
var aksName = '${namePrefix}-aks'
var sqlServerName = '${namePrefix}-sql-${uniqueSuffix}'
var redisName = '${namePrefix}-redis-${uniqueSuffix}'
var serviceBusNamespaceName = '${namePrefix}-sb-${uniqueSuffix}'
var databaseNames = ['CatalogDb', 'IdentityDb', 'CartDb', 'OrderDb']

// ---- Container Registry ----

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: { name: 'Basic' }
  properties: { adminUserEnabled: false }
}

// ---- AKS: single small node pool, free control plane ----

resource aks 'Microsoft.ContainerService/managedClusters@2024-05-01' = {
  name: aksName
  location: location
  identity: { type: 'SystemAssigned' }
  sku: { name: 'Base', tier: 'Free' } // field name changed from "Basic" in recent API versions — double-check against current schema
  properties: {
    dnsPrefix: '${namePrefix}-aks-dns'
    agentPoolProfiles: [
      {
        name: 'systempool'
        count: 1
        vmSize: 'Standard_B2s'
        mode: 'System'
        osType: 'Linux'
        type: 'VirtualMachineScaleSets'
      }
    ]
  }
}

// Lets AKS pull images from ACR without a separate imagePullSecret.
resource acrPullRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, aks.id, 'AcrPull')
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') // built-in AcrPull role
    principalId: aks.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}

// ---- Azure SQL: one logical server, one Basic database per service ----

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
  }
}

resource sqlFirewallAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabases 'Microsoft.Sql/servers/databases@2023-05-01-preview' = [
  for dbName in databaseNames: {
    parent: sqlServer
    name: dbName
    location: location
    sku: { name: 'Basic', tier: 'Basic' }
    properties: { maxSizeBytes: 2147483648 } // 2 GB — Basic tier max
  }
]

// ---- Redis (Catalog's cache-aside) ----

resource redis 'Microsoft.Cache/redis@2024-03-01' = {
  name: redisName
  location: location
  properties: {
    sku: { name: 'Basic', family: 'C', capacity: 0 }
    enableNonSslPort: false
  }
}

// ---- Service Bus (Order → OrderProcessing) ----

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  sku: { name: 'Basic', tier: 'Basic' }
}

resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'order-placed'
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 3
  }
}

resource serviceBusAuthRule 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2022-10-01-preview' existing = {
  parent: serviceBusNamespace
  name: 'RootManageSharedAccessKey'
}

output acrLoginServer string = acr.properties.loginServer
output aksName string = aks.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output redisHostName string = redis.properties.hostName
output serviceBusNamespaceFqdn string = '${serviceBusNamespace.name}.servicebus.windows.net'
output serviceBusAuthRuleId string = serviceBusAuthRule.id
