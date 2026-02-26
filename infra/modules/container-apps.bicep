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

@description('Project name used for resource naming')
param projectName string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: containerAppsSubnetId
      internal: false
    }
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
  name: '${projectName}-web'
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
        targetPort: 3000
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
          image: '${acrLoginServer}/${projectName}-web:${imageTag}'
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
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/healthz'
                port: 3000
              }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/ready'
                port: 3000
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
  name: '${projectName}-api'
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
          image: '${acrLoginServer}/${projectName}-api:${imageTag}'
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
  name: '${projectName}-mcp-gateway'
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
        targetPort: 8090
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
          image: '${acrLoginServer}/${projectName}-mcp-gateway:${imageTag}'
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
                port: 8090
              }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/ready'
                port: 8090
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
