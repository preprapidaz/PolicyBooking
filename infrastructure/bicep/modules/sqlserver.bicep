@description('SQL Server name')
param sqlServerName string

@description('SQL Database name')
param databaseName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('SQL Admin username')
param administratorLogin string

@description('SQL Admin password')
@secure()
param administratorPassword string

@description('Database SKU')
param databaseSku object = { name: 'Basic', tier: 'Basic', capacity: 5 }

@description('Tags for the resource')
param tags object = {}

resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    version: '12.0'
    publicNetworkAccess: 'Enabled'
  }
}

resource firewallRuleAzure 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource database 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: databaseSku
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Local'
  }
}

output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
#disable-next-line outputs-should-not-contain-secrets
output connectionString string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};Persist Security Info=False;User ID=${administratorLogin};Password=${administratorPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
