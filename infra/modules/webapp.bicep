@description('Region for the App Service plan and web app')
param location string

@description('Display name of the region, used by the app to pick its nearest Cosmos replica')
param regionDisplayName string

param appServicePlanName string
param webAppName string

@description('Cosmos DB account endpoint')
param cosmosEndpoint string

param cosmosDatabase string = 'StyleVerseDb'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      appCommandLine: 'dotnet StyleVerse.Backend.dll'
      alwaysOn: true
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      healthCheckPath: '/api/health'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'App__Region'
          value: regionDisplayName
        }
        {
          name: 'Cosmos__Endpoint'
          value: cosmosEndpoint
        }
        {
          name: 'Cosmos__Database'
          value: cosmosDatabase
        }
      ]
    }
  }
}

output name string = webApp.name
output hostName string = webApp.properties.defaultHostName
output principalId string = webApp.identity.principalId
