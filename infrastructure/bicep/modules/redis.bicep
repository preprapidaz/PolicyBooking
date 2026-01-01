@description('Redis Cache name')
param redisName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Redis SKU')
@allowed(['Basic','Standard','Premium'])
param sku string = 'Basic'

@description('Redis family')
@allowed(['C','P'])
param family string = 'C'

@description('Redis capacity')
@allowed([0,1,2,3,4,5,6])
param capacity int = 0

@description('Enable non-SSL port')
param enableNonSslPort bool = false

@description('Tags for the resource')
param tags object = {}

resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisName
  location: location
  tags: tags
  properties: {
    sku: { name: sku, family: family, capacity: capacity }
    enableNonSslPort: enableNonSslPort
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    redisConfiguration: { 'maxmemory-policy': 'allkeys-lru' }
  }
}

output hostName string = redis.properties.hostName
output sslPort int = redis.properties.sslPort
#disable-next-line outputs-should-not-contain-secrets
output primaryKey string = redis.listKeys().primaryKey
#disable-next-line outputs-should-not-contain-secrets
output connectionString string = '${redis.properties.hostName}:${redis.properties.sslPort},password=${redis.listKeys().primaryKey},ssl=True,abortConnect=False'
