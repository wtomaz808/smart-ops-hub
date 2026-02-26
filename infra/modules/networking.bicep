// Module: Virtual Network with subnets for Container Apps, SQL, and Private Endpoints

@description('Azure region for resource deployment')
param location string

@description('Name of the virtual network')
param vnetName string

@description('Tags to apply to all resources')
param tags object

@description('Address prefix for the virtual network')
param vnetAddressPrefix string = '10.0.0.0/16'

@description('Address prefix for the Container Apps subnet')
param containerAppsSubnetPrefix string = '10.0.0.0/21'

@description('Address prefix for the SQL subnet')
param sqlSubnetPrefix string = '10.0.8.0/24'

@description('Address prefix for the Private Endpoints subnet')
param privateEndpointsSubnetPrefix string = '10.0.9.0/24'

resource vnet 'Microsoft.Network/virtualNetworks@2023-05-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        vnetAddressPrefix
      ]
    }
    subnets: [
      {
        name: 'snet-container-apps'
        properties: {
          addressPrefix: containerAppsSubnetPrefix
          delegations: [
            {
              name: 'Microsoft.App.environments'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
      {
        name: 'snet-sql'
        properties: {
          addressPrefix: sqlSubnetPrefix
          serviceEndpoints: [
            {
              service: 'Microsoft.Sql'
            }
          ]
        }
      }
      {
        name: 'snet-private-endpoints'
        properties: {
          addressPrefix: privateEndpointsSubnetPrefix
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
    ]
  }
}

@description('Resource ID of the virtual network')
output vnetId string = vnet.id

@description('Resource ID of the Container Apps subnet')
output containerAppsSubnetId string = vnet.properties.subnets[0].id

@description('Resource ID of the SQL subnet')
output sqlSubnetId string = vnet.properties.subnets[1].id

@description('Resource ID of the Private Endpoints subnet')
output privateEndpointsSubnetId string = vnet.properties.subnets[2].id
