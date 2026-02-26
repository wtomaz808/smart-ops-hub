// Module: Azure SQL Server and Database with AAD-only authentication

@description('Azure region for resource deployment')
param location string

@description('Name of the SQL Server')
param sqlServerName string

@description('Name of the SQL Database')
param sqlDatabaseName string

@description('Tags to apply to all resources')
param tags object

@description('Principal ID of the managed identity to set as AAD admin')
param principalId string

@description('Client ID of the managed identity')
param identityClientId string

@description('Name of the managed identity for AAD admin display')
param identityName string

@description('Resource ID of the user-assigned managed identity')
param identityId string

@description('SKU name for the SQL Database')
param skuName string = 'S1'

@description('SKU tier for the SQL Database')
param skuTier string = 'Standard'

@description('Resource ID of the Log Analytics workspace for diagnostics')
param logAnalyticsWorkspaceId string

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    primaryUserAssignedIdentityId: identityId
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: identityName
      sid: principalId
      principalType: 'Application'
      tenantId: subscription().tenantId
    }
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
  }
}

resource sqlDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${sqlDatabaseName}-diag'
  scope: sqlDatabase
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

@description('Fully qualified domain name of the SQL Server')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Resource ID of the SQL Server')
output sqlServerId string = sqlServer.id

@description('Name of the SQL Database')
output databaseName string = sqlDatabase.name

@description('Client ID of the managed identity for AAD connection strings')
output adminIdentityClientId string = identityClientId
