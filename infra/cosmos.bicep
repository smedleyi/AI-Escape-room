targetScope = 'resourceGroup'

@description('Azure region for the Cosmos DB account')
param location string = 'uksouth'

@description('Globally unique Cosmos DB account name')
param accountName string = 'cosmos-styleverse-${uniqueString(resourceGroup().id)}'

@description('Maximum autoscale throughput per container (scales down to 10% of this)')
@minValue(1000)
param autoscaleMaxThroughput int = 1000

@description('Web app whose managed identity gets Cosmos data read/write access')
param webAppName string = 'web-styleverse-${uniqueString(resourceGroup().id)}'

@description('Optional Entra object IDs (e.g. developers) that also get data read/write access')
param extraDataContributorIds array = []

var databaseName = 'StyleVerseDb'

var containers = [
  {
    name: 'Products'
    partitionKey: '/id'
  }
  {
    name: 'Carts'
    partitionKey: '/sessionId'
  }
  {
    name: 'Orders'
    partitionKey: '/email'
  }
]

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: accountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    minimalTlsVersion: 'Tls12'
    publicNetworkAccess: 'Enabled'
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-05-15' = {
  parent: account
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource sqlContainers 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = [for c in containers: {
  parent: database
  name: c.name
  properties: {
    resource: {
      id: c.name
      partitionKey: {
        paths: [
          c.partitionKey
        ]
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
    }
    options: {
      autoscaleSettings: {
        maxThroughput: autoscaleMaxThroughput
      }
    }
  }
}]

resource webApp 'Microsoft.Web/sites@2023-01-01' existing = {
  name: webAppName
}

// Built-in "Cosmos DB Built-in Data Contributor" role
var dataContributorRoleId = '${account.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'

resource webAppDataContributor 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = {
  parent: account
  name: guid(account.id, webApp.id, dataContributorRoleId)
  properties: {
    roleDefinitionId: dataContributorRoleId
    principalId: webApp.identity.principalId
    scope: account.id
  }
}

resource extraDataContributors 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = [for principalId in extraDataContributorIds: {
  parent: account
  name: guid(account.id, principalId, dataContributorRoleId)
  properties: {
    roleDefinitionId: dataContributorRoleId
    principalId: principalId
    scope: account.id
  }
}]

output accountName string = account.name
output endpoint string = account.properties.documentEndpoint
output databaseName string = database.name
