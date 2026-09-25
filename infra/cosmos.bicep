targetScope = 'resourceGroup'

@description('Primary region of the Cosmos DB account')
param location string = 'uksouth'

@description('Replica regions in failover order. Zone redundancy cannot be changed on an existing region.')
param regions array = [
  {
    locationName: location
    failoverPriority: 0
    isZoneRedundant: false
  }
  {
    locationName: 'eastus2'
    failoverPriority: 1
    isZoneRedundant: true
  }
  {
    locationName: 'eastasia'
    failoverPriority: 2
    isZoneRedundant: true
  }
]

@description('Globally unique Cosmos DB account name')
param accountName string = 'cosmos-styleverse-${uniqueString(resourceGroup().id)}'

@description('Maximum autoscale throughput per container (scales down to 10% of this)')
@minValue(1000)
param autoscaleMaxThroughput int = 4000

@description('Web apps whose managed identities get Cosmos data read/write access (one per region)')
param webAppNames array = [
  'web-styleverse-${uniqueString(resourceGroup().id)}'
  'web-styleverse-cus-${uniqueString(resourceGroup().id)}'
  'web-styleverse-eas-${uniqueString(resourceGroup().id)}'
]

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
    // Session: each shopper reads their own writes while every region serves local reads and writes.
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: regions
    enableMultipleWriteLocations: true
    enableAutomaticFailover: true
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
      // Multi-region writes: concurrent edits to the same document resolve to the latest write.
      conflictResolutionPolicy: {
        mode: 'LastWriterWins'
        conflictResolutionPath: '/_ts'
      }
    }
    options: {
      autoscaleSettings: {
        maxThroughput: autoscaleMaxThroughput
      }
    }
  }
}]

resource webApps 'Microsoft.Web/sites@2023-01-01' existing = [for name in webAppNames: {
  name: name
}]

// Built-in "Cosmos DB Built-in Data Contributor" role
var dataContributorRoleId = '${account.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'

resource webAppDataContributors 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-05-15' = [for (name, i) in webAppNames: {
  parent: account
  name: guid(account.id, resourceId('Microsoft.Web/sites', name), dataContributorRoleId)
  properties: {
    roleDefinitionId: dataContributorRoleId
    principalId: webApps[i].identity.principalId
    scope: account.id
  }
}]

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
