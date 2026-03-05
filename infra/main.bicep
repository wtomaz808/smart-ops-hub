// Main Bicep orchestrator for AgentOpsHub — Azure Government deployment
targetScope = 'resourceGroup'

// ─── Parameters ─────────────────────────────────────────────────────────────────

@description('Name of the deployment environment (e.g., dev, staging, prod)')
param environmentName string

@description('Azure region for all resources — defaults to Azure Government Arizona')
param location string = 'usgovarizona'

@description('Container image tag to deploy')
param imageTag string = 'latest'

@description('SKU for Azure SQL Database')
param sqlSkuName string = 'S1'

@description('SKU tier for Azure SQL Database')
param sqlSkuTier string = 'Standard'

@description('Log Analytics retention in days')
@minValue(30)
@maxValue(730)
param logRetentionDays int = 90

@description('TPM capacity (thousands) for GPT-4o deployment')
param gpt4oCapacity int = 10

@description('TPM capacity (thousands) for GPT-4.1 deployment')
param gpt41Capacity int = 10

@description('Entra ID app registration client ID for site authentication (created by setup-app-registration.ps1)')
param entraAppClientId string = ''

@description('Entra ID tenant ID for authentication')
param entraTenantId string = ''

// ─── Variables ──────────────────────────────────────────────────────────────────

// Naming: aoh-{type}-{env}  (no hyphens for ACR, Key Vault, Storage)
var prefix = 'aoh'
var env = environmentName
var tags = {
  environment: environmentName
  project: 'AgentOpsHub'
  managedBy: 'bicep'
}

// ─── Modules ────────────────────────────────────────────────────────────────────

// 1. User-Assigned Managed Identity
module identity 'modules/identity.bicep' = {
  name: 'identity'
  params: {
    location: location
    identityName: '${prefix}-id-${env}'
    tags: tags
  }
}

// 2. Virtual Network
module networking 'modules/networking.bicep' = {
  name: 'networking'
  params: {
    location: location
    vnetName: '${prefix}-vnet-${env}'
    tags: tags
  }
}

// 3. Monitoring (Log Analytics + Application Insights)
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    logAnalyticsName: '${prefix}-log-${env}'
    appInsightsName: '${prefix}-appi-${env}'
    tags: tags
    retentionInDays: logRetentionDays
  }
}

// 4. Azure Container Registry (no hyphens)
module acr 'modules/acr.bicep' = {
  name: 'acr'
  params: {
    location: location
    acrName: '${prefix}acr${env}'
    tags: tags
    principalId: identity.outputs.principalId
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
  }
}

// 5. Key Vault (no hyphens)
module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    keyVaultName: '${prefix}kv${env}'
    tags: tags
    principalId: identity.outputs.principalId
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
  }
}

// 6. Azure SQL
module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    location: location
    sqlServerName: '${prefix}-sql-${env}'
    sqlDatabaseName: '${prefix}-db-${env}'
    tags: tags
    principalId: identity.outputs.principalId
    identityClientId: identity.outputs.clientId
    identityName: identity.outputs.identityName
    identityId: identity.outputs.identityId
    skuName: sqlSkuName
    skuTier: sqlSkuTier
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
  }
}

// 7. Azure OpenAI
module openai 'modules/openai.bicep' = {
  name: 'openai'
  params: {
    location: location
    openAiName: '${prefix}-oai-${env}'
    tags: tags
    principalId: identity.outputs.principalId
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
    gpt4oCapacity: gpt4oCapacity
    gpt41Capacity: gpt41Capacity
  }
}

// 8. Azure AI Services
module aiServices 'modules/ai-services.bicep' = {
  name: 'ai-services'
  params: {
    location: location
    aiServicesName: '${prefix}-ais-${env}'
    tags: tags
    principalId: identity.outputs.principalId
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
  }
}

// 9. Container Apps
module containerApps 'modules/container-apps.bicep' = {
  name: 'container-apps'
  params: {
    location: location
    environmentName: '${prefix}-cae-${env}'
    tags: tags
    containerAppsSubnetId: networking.outputs.containerAppsSubnetId
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
    logAnalyticsCustomerId: monitoring.outputs.logAnalyticsCustomerId
    logAnalyticsSharedKey: monitoring.outputs.logAnalyticsSharedKey
    identityId: identity.outputs.identityId
    identityClientId: identity.outputs.clientId
    acrLoginServer: acr.outputs.loginServer
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    openAiEndpoint: openai.outputs.openAiEndpoint
    sqlServerFqdn: sql.outputs.sqlServerFqdn
    sqlDatabaseName: sql.outputs.databaseName
    keyVaultUri: keyvault.outputs.keyVaultUri
    aiServicesEndpoint: aiServices.outputs.aiServicesEndpoint
    imageTag: imageTag
    prefix: prefix
    env: env
    entraAppClientId: entraAppClientId
    entraTenantId: entraTenantId
  }
}

// ─── Outputs ────────────────────────────────────────────────────────────────────

@description('FQDN of the deployed web application')
output webAppFqdn string = containerApps.outputs.webAppFqdn

@description('FQDN of the deployed API application')
output apiAppFqdn string = containerApps.outputs.apiAppFqdn

@description('FQDN of the deployed MCP Gateway')
output mcpGatewayFqdn string = containerApps.outputs.mcpGatewayFqdn

@description('Azure OpenAI endpoint')
output openAiEndpoint string = openai.outputs.openAiEndpoint

@description('Azure SQL Server FQDN')
output sqlServerFqdn string = sql.outputs.sqlServerFqdn

@description('Key Vault URI')
output keyVaultUri string = keyvault.outputs.keyVaultUri

@description('Managed Identity Client ID')
output identityClientId string = identity.outputs.clientId
