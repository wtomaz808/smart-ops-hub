// Module: User-Assigned Managed Identity

@description('Azure region for resource deployment')
param location string

@description('Name of the managed identity resource')
param identityName string

@description('Tags to apply to all resources')
param tags object

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

@description('Resource ID of the managed identity')
output identityId string = managedIdentity.id

@description('Principal ID of the managed identity')
output principalId string = managedIdentity.properties.principalId

@description('Client ID of the managed identity')
output clientId string = managedIdentity.properties.clientId

@description('Name of the managed identity')
output identityName string = managedIdentity.name
