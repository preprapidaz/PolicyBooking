@description('Name of the container group')
param containerGroupName string

@description('Container image')
param image string

@description('Location for the resource')
param location string = resourceGroup().location

@description('Number of CPU cores')
param cpuCores int = 1

@description('Memory in GB')
param memoryInGb int = 1

@description('Container port')
param containerPort int = 8080

@description('DNS name label (for public access)')
param dnsNameLabel string = ''

@description('Environment variables')
param environmentVariables array = []

@description('ACR login server')
param acrLoginServer string

@description('ACR username')
@secure()
param acrUsername string

@description('ACR password')
@secure()
param acrPassword string

@description('Managed Identity resource ID')
param identityId string = ''

@description('Restart policy')
@allowed(['Always','Never','OnFailure'])
param restartPolicy string = 'Always'

@description('Tags for the resource')
param tags object = {}

var hasPublicIp = dnsNameLabel != ''
var hasIdentity = identityId != ''

resource containerGroup 'Microsoft.ContainerInstance/containerGroups@2023-05-01' = {
  name: containerGroupName
  location: location
  tags: tags
  identity: hasIdentity ? {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  } : null
  properties: {
    containers: [
      {
        name: containerGroupName
        properties: {
          image: image
          ports: hasPublicIp ? [
            {
              port: containerPort
              protocol: 'TCP'
            }
          ] : []
          environmentVariables: environmentVariables
          resources: {
            requests: {
              cpu: cpuCores
              memoryInGB: memoryInGb
            }
          }
        }
      }
    ]
    osType: 'Linux'
    restartPolicy: restartPolicy
    imageRegistryCredentials: [
      {
        server: acrLoginServer
        username: acrUsername
        password: acrPassword
      }
    ]
    ipAddress: hasPublicIp ? {
      type: 'Public'
      dnsNameLabel: dnsNameLabel
      ports: [
        {
          port: containerPort
          protocol: 'TCP'
        }
      ]
    } : null
  }
}

output fqdn string = hasPublicIp ? containerGroup.properties.ipAddress.fqdn : ''
output ipAddress string = hasPublicIp ? containerGroup.properties.ipAddress.ip : ''
output name string = containerGroup.name
