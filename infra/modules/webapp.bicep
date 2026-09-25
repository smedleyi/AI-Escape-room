@description('Region for the App Service plan and web app')
param location string

@description('Display name of the region, used by the app to pick its nearest Cosmos replica')
param regionDisplayName string

param appServicePlanName string
param webAppName string

@description('Cosmos DB account endpoint')
param cosmosEndpoint string

param cosmosDatabase string = 'StyleVerseDb'

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Log Analytics workspace for App Service HTTP and console logs')
param logAnalyticsWorkspaceId string

@description('Upper bound for automatic scale-out of this region')
@minValue(1)
param maxInstances int = 10

// Premium v3 is required for automatic scaling: App Service adds pre-warmed instances as HTTP load rises.
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'P0v3'
    tier: 'PremiumV3'
    capacity: 1
  }
  properties: {
    reserved: true
    elasticScaleEnabled: true
    maximumElasticWorkerCount: maxInstances
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
    // The app keeps no in-memory session state, so don't pin users to one instance.
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      appCommandLine: 'dotnet StyleVerse.Backend.dll'
      alwaysOn: true
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
      healthCheckPath: '/api/health'
      minimumElasticInstanceCount: 1
      elasticWebAppScaleLimit: maxInstances
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'OTEL_SERVICE_NAME'
          value: webAppName
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

resource webAppDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'to-log-analytics'
  scope: webApp
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled: true
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled: true
      }
    ]
  }
}

output name string = webApp.name
output hostName string = webApp.properties.defaultHostName
output principalId string = webApp.identity.principalId
