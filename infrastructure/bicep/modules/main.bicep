targetScope = 'subscription'

@description('Environment name (dev, staging, production)')
param environment string

@description('Location for all resources')
param location string = 'eastus'

@description('Project name prefix')
param projectName string = 'policyservice'

@description('SQL Admin username')
param sqlAdminUsername string = 'sqladmin'

@description('SQL Admin password')
@secure()
param sqlAdminPassword string

@description('Deploy API container')
param deployApi bool = false

@description('Deploy Worker container')
param deployWorker bool = false

@description('API Docker image tag')
param apiImageTag string = 'latest'

@description('Worker Docker image tag')
param workerImageTag string = 'latest'

// Generate unique names with suffix
var uniqueSuffix = uniqueString(subscription().id, environment, location)
var resourceGroupName = '${projectName}-${environment}-rg'
var acrName = toLower('${projectName}acr${uniqueSuffix}')
var sqlServerName = toLower('${projectName}-sql-${uniqueSuffix}')
var redisName = toLower('${projectName}-redis-${uniqueSuffix}')
var keyVaultName = toLower('${projectName}-kv-${uniqueSuffix}')
var identityName = '${projectName}-${environment}-identity'
var apiContainerName = '${projectName}-api-${environment}'
var workerContainerName = '${projectName}-worker-${environment}'

var tags = {
  Environment: environment
  Project: projectName
  ManagedBy: 'Bicep'
}

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

// Managed Identity
module identity './modules/managed-identity.bicep' = {
  scope: rg
  name: 'deploy-identity'
  params: {
    identityName: identityName
    location: location
    tags: tags
  }
}

// Azure Container Registry
module acr './modules/acr.bicep' = {S
  scope: rg
  name: 'deploy-acr'
  params: {
    acrName: acrName
    location: location
    sku: 'Basic'
    tags: tags
  }
}

// SQL Server & Database
module sql './modules/sqlserver.bicep' = {
  scope: rg
  name: 'deploy-sql'
  params: {
    sqlServerName: sqlServerName
    databaseName: '${projectName}db'
    location: location
    administratorLogin: sqlAdminUsername
    administratorPassword: sqlAdminPassword
    tags: tags
  }
}

// Redis Cache
module redis './modules/redis.bicep' = {
  scope: rg
  name: 'deploy-redis'
  params: {
    redisName: redisName
    location: location
    sku: 'Basic'
    family: 'C'
    capacity: 0
    tags: tags
  }
}

// Key Vault
module keyVault './modules/keyvault.bicep' = {
  scope: rg
  name: 'deploy-keyvault'
  params: {
    keyVaultName: keyVaultName
    location: location
    managedIdentityPrincipalId: identity.outputs.principalId
    tags: tags
  }
}

// Get ACR credentials for ACI
resource acrResource 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  scope: rg
  name: acrName
}

// API Container (optional)
module apiContainer './modules/aci.bicep' = if (deployApi) {
  scope: rg
  name: 'deploy-api-container'
  params: {
    containerGroupName: apiContainerName
    image: '${acr.outputs.loginServer}/${projectName}-api:${apiImageTag}'
    location: location
    cpuCores: 1
    memoryInGb: 2
    containerPort: 8080
    dnsNameLabel: '${projectName}-api-${uniqueSuffix}'
    acrLoginServer: acr.outputs.loginServer
    acrUsername: listCredentials(acrResource.id, '2023-01-01-preview').username
    acrPassword: listCredentials(acrResource.id, '2023-01-01-preview').passwords[0].value
    identityId: identity.outputs.id
    environmentVariables: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: environment
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
      {
        name: 'KeyVaultName'
        value: keyVaultName
      }
    ]
    tags: tags
  }
}

// Worker Container (optional)
module workerContainer './modules/aci.bicep' = if (deployWorker) {
  scope: rg
  name: 'deploy-worker-container'
  params: {
    containerGroupName: workerContainerName
    image: '${acr.outputs.loginServer}/${projectName}-worker:${apiImageTag}'
    location: location
    cpuCores: 1
    memoryInGb: 1
    containerPort: 8080
    acrLoginServer: acr.outputs.loginServer
    acrUsername: listCredentials(acrResource.id, '2023-01-01-preview').username
    acrPassword: listCredentials(acrResource.id, '2023-01-01-preview').passwords[0].value
    identityId: identity.outputs.id
    environmentVariables: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: environment
      }
      {
        name: 'KeyVaultName'
        value: keyVaultName
      }
    ]
    tags: tags
  }
}

// Outputs
output resourceGroupName string = rg.name
output acrLoginServer string = acr.outputs.loginServer
output acrName string = acr.outputs.name
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
output keyVaultUri string = keyVault.outputs.vaultUri
output keyVaultName string = keyVault.outputs.name
output identityClientId string = identity.outputs.clientId
output apiUrl string = deployApi ? 'http://${apiContainer.outputs.fqdn}:8080' : 'Not deployed'
output apiSwaggerUrl string = deployApi ? 'http://${apiContainer.outputs.fqdn}:8080/swagger' : 'Not deployed'
