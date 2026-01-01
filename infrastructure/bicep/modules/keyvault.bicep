@description('Key Vault name')
param keyVaultName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('SKU name')
@allowed(['standard','premium'])
param skuName string = 'standard'

@description('Tenant ID')
param tenantId string = subscription().tenantId

@description('Object ID of the managed identity to grant access')
param managedIdentityPrincipalId string = ''

@description('Tags for the resource')
param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: { family: 'A', name: skuName }
    tenantId: tenantId
    enableRbacAuthorization: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enablePurgeProtection: false
    publicNetworkAccess: 'Enabled'
    accessPolicies: managedIdentityPrincipalId != '' ? [{
      tenantId: tenantId
      objectId: managedIdentityPrincipalId
      permissions: { secrets: ['get','list'] }
    }] : []
  }
}

output vaultUri string = keyVault.properties.vaultUri
output name string = keyVault.name
output id string = keyVault.id
