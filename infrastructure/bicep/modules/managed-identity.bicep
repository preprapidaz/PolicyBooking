@description('Name of the managed identity')
param identityName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Tags for the resource')
param tags object = {}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

output id string = identity.id
output clientId string = identity.properties.clientId
output principalId string = identity.properties.principalId
