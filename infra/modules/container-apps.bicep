// Module: Container Apps Environment and Container Apps (web, api, mcp-gateway)

@description('Azure region for resource deployment')
param location string

@description('Name of the Container Apps environment')
param environmentName string

@description('Tags to apply to all resources')
param tags object

@description('Resource ID of the Container Apps subnet')
param containerAppsSubnetId string

@description('Resource ID of the Log Analytics workspace')
param logAnalyticsWorkspaceId string

@description('Customer ID of the Log Analytics workspace')
param logAnalyticsCustomerId string

@secure()
@description('Shared key of the Log Analytics workspace')
param logAnalyticsSharedKey string

@description('Resource ID of the user-assigned managed identity')
param identityId string

@description('Login server URL for ACR')
param acrLoginServer string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Azure OpenAI endpoint URL')
param openAiEndpoint string

@description('Azure SQL Server FQDN')
param sqlServerFqdn string

@description('Name of the SQL Database')
param sqlDatabaseName string

@description('Key Vault URI')
param keyVaultUri string

@description('Azure AI Services endpoint URL')
param aiServicesEndpoint string

@description('Container image tag to deploy')
param imageTag string = 'latest'

@description('Naming prefix (e.g., aoh)')
param prefix string

@description('Environment name (e.g., dev, prod)')
param env string

@description('Entra ID app registration client ID for site authentication')
param entraAppClientId string = ''

@description('Entra ID tenant ID for authentication')
param entraTenantId string = ''

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsSharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: containerAppsSubnetId
      internal: false
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    zoneRedundant: false
  }
}

resource containerAppsEnvDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${environmentName}-diag'
  scope: containerAppsEnvironment
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
  }
}

// --- Web App ---
resource webApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${prefix}-ca-web-${env}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      registries: [
        {
          server: acrLoginServer
          identity: identityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'web'
          image: '${acrLoginServer}/aoh-web:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'AZURE_KEYVAULT_URI'
              value: keyVaultUri
            }
            {
              name: 'ApiBaseUrl'
              value: 'https://${prefix}-ca-api-${env}.${containerAppsEnvironment.properties.defaultDomain}'
            }
            {
              name: 'AzureAd__Instance'
              value: 'https://login.microsoftonline.us/'
            }
            {
              name: 'AzureAd__TenantId'
              value: entraTenantId
            }
            {
              name: 'AzureAd__ClientId'
              value: entraAppClientId
            }
            {
              name: 'AzureAd__CallbackPath'
              value: '/signin-oidc'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/healthz'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/ready'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 15
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
      }
    }
  }
}

// --- API App ---
resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${prefix}-ca-api-${env}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      registries: [
        {
          server: acrLoginServer
          identity: identityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${acrLoginServer}/aoh-api:${imageTag}'
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
          env: [
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'AZURE_OPENAI_ENDPOINT'
              value: openAiEndpoint
            }
            {
              name: 'AZURE_SQL_SERVER'
              value: sqlServerFqdn
            }
            {
              name: 'AZURE_SQL_DATABASE'
              value: sqlDatabaseName
            }
            {
              name: 'AZURE_KEYVAULT_URI'
              value: keyVaultUri
            }
            {
              name: 'AZURE_AI_SERVICES_ENDPOINT'
              value: aiServicesEndpoint
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: 'https://${prefix}-ca-web-${env}.${containerAppsEnvironment.properties.defaultDomain}'
            }
            {
              name: 'AzureAd__Instance'
              value: 'https://login.microsoftonline.us/'
            }
            {
              name: 'AzureAd__TenantId'
              value: entraTenantId
            }
            {
              name: 'AzureAd__ClientId'
              value: entraAppClientId
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/healthz'
                port: 8080
              }
              initialDelaySeconds: 15
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/ready'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 15
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
      }
    }
  }
}

// --- MCP Gateway App ---
resource mcpGatewayApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: '${prefix}-ca-gw-${env}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      registries: [
        {
          server: acrLoginServer
          identity: identityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'mcp-gateway'
          image: '${acrLoginServer}/aoh-gw:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsightsConnectionString
            }
            {
              name: 'AZURE_OPENAI_ENDPOINT'
              value: openAiEndpoint
            }
            {
              name: 'AZURE_KEYVAULT_URI'
              value: keyVaultUri
            }
            {
              name: 'AZURE_AI_SERVICES_ENDPOINT'
              value: aiServicesEndpoint
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/healthz'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/ready'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 15
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
      }
    }
  }
}

@description('FQDN of the web container app')
output webAppFqdn string = webApp.properties.configuration.ingress.fqdn

@description('FQDN of the API container app')
output apiAppFqdn string = apiApp.properties.configuration.ingress.fqdn

@description('FQDN of the MCP Gateway container app')
output mcpGatewayFqdn string = mcpGatewayApp.properties.configuration.ingress.fqdn

@description('Resource ID of the Container Apps environment')
output environmentId string = containerAppsEnvironment.id
