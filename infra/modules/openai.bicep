// Module: Azure OpenAI with gpt-4o deployment and managed identity access

@description('Azure region for resource deployment')
param location string

@description('Name of the Azure OpenAI account')
param openAiName string

@description('Tags to apply to all resources')
param tags object

@description('Principal ID of the managed identity to assign Cognitive Services User role')
param principalId string

@description('SKU for the OpenAI account')
param sku string = 'S0'

@description('Resource ID of the Log Analytics workspace for diagnostics')
param logAnalyticsWorkspaceId string

@description('Capacity (in thousands of TPM) for gpt-4o deployment')
param gpt4oCapacity int = 10

resource openAi 'Microsoft.CognitiveServices/accounts@2024-04-01-preview' = {
  name: openAiName
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: sku
  }
  properties: {
    customSubDomainName: openAiName
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
}

resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-04-01-preview' = {
  parent: openAi
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: gpt4oCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-08-06'
    }
    raiPolicyName: 'Microsoft.DefaultV2'
  }
}

// Cognitive Services User: a97b65f3-24c7-4388-baec-2e87135dc908
var cognitiveServicesUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')

resource openAiRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAi.id, principalId, cognitiveServicesUserRoleId)
  scope: openAi
  properties: {
    roleDefinitionId: cognitiveServicesUserRoleId
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

resource openAiDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${openAiName}-diag'
  scope: openAi
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

@description('Endpoint URL for the Azure OpenAI account')
output openAiEndpoint string = openAi.properties.endpoint

@description('Resource ID of the Azure OpenAI account')
output openAiId string = openAi.id

@description('Name of the Azure OpenAI account')
output openAiName string = openAi.name
