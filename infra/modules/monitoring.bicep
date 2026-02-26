// Module: Log Analytics Workspace and Application Insights

@description('Azure region for resource deployment')
param location string

@description('Name of the Log Analytics workspace')
param logAnalyticsName string

@description('Name of the Application Insights resource')
param appInsightsName string

@description('Tags to apply to all resources')
param tags object

@description('Retention period in days for Log Analytics')
@minValue(30)
@maxValue(730)
param retentionInDays int = 90

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionInDays
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

@description('Resource ID of the Log Analytics workspace')
output logAnalyticsWorkspaceId string = logAnalytics.id

@description('Customer ID of the Log Analytics workspace')
output logAnalyticsCustomerId string = logAnalytics.properties.customerId

@description('Resource ID of the Application Insights')
output appInsightsId string = appInsights.id

@description('Instrumentation key of Application Insights')
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey

@description('Connection string for Application Insights')
output appInsightsConnectionString string = appInsights.properties.ConnectionString
