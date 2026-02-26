// Module: Azure AI Services (multi-service account) with managed identity access

@description('Azure region for resource deployment')
param location string

@description('Name of the Azure AI Services account')
param aiServicesName string

@description('Tags to apply to all resources')
param tags object

@description('Principal ID of the managed identity to assign Cognitive Services User role')
param principalId string

@description('SKU for the AI Services account')
param sku string = 'S0'

@description('Resource ID of the Log Analytics workspace for diagnostics')
param logAnalyticsWorkspaceId string

resource aiServices 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' = {
  name: aiServicesName
  location: location
  tags: tags
  kind: 'CognitiveServices'
  sku: {
    name: sku
  }
  properties: {
    customSubDomainName: aiServicesName
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
}

// Cognitive Services User: a97b65f3-24c7-4388-baec-2e87135dc908
var cognitiveServicesUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')

resource aiServicesRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, principalId, cognitiveServicesUserRoleId)
  scope: aiServices
  properties: {
    roleDefinitionId: cognitiveServicesUserRoleId
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

resource aiServicesDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${aiServicesName}-diag'
  scope: aiServices
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

@description('Endpoint URL for the Azure AI Services account')
output aiServicesEndpoint string = aiServices.properties.endpoint

@description('Resource ID of the Azure AI Services account')
output aiServicesId string = aiServices.id

@description('Name of the Azure AI Services account')
output aiServicesName string = aiServices.name
