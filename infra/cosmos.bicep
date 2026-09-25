targetScope = 'resourceGroup'

@description('Azure region for the Cosmos DB account')
param location string = 'uksouth'

@description('Globally unique Cosmos DB account name')
param accountName string = 'cosmos-styleverse-${uniqueString(resourceGroup().id)}'

@description('Maximum autoscale throughput per container (scales down to 10% of this)')
@minValue(1000)
param autoscaleMaxThroughput int = 1000

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

output accountName string = account.name
output endpoint string = account.properties.documentEndpoint
output databaseName string = database.name
