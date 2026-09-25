targetScope = 'resourceGroup'

@description('Azure region for the deployment')
param location string = resourceGroup().location

@description('SQL administrator login name')
param sqlAdminLogin string = 'styleverseadmin'

@secure()
@description('SQL administrator password')
param sqlAdminPassword string

@description('Name of the Azure SQL logical server')
param sqlServerName string = 'sql-styleverse-${uniqueString(resourceGroup().id)}'

@description('Name of the SQL database')
param sqlDatabaseName string = 'StyleVerseDb'

@description('Cosmos DB account the apps read and write')
param cosmosAccountName string = 'cosmos-styleverse-${uniqueString(resourceGroup().id)}'

@description('Application Insights and Log Analytics created by monitoring.bicep')
param appInsightsName string = 'appi-styleverse-${uniqueString(resourceGroup().id)}'
param logAnalyticsWorkspaceName string = 'log-styleverse-${uniqueString(resourceGroup().id)}'

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  name: logAnalyticsWorkspaceName
}

@description('One App Service per region, each paired with its nearest Cosmos replica')
param webRegions array = [
  {
    location: 'westeurope'
    displayName: 'West Europe'
    planName: 'asp-styleverse-${uniqueString(resourceGroup().id)}'
    appName: 'web-styleverse-${uniqueString(resourceGroup().id)}'
  }
  // East US 2 has no App Service quota in this subscription; Central US is ~25 ms from the East US 2 Cosmos replica.
  {
    location: 'centralus'
    displayName: 'Central US'
    planName: 'asp-styleverse-cus-${uniqueString(resourceGroup().id)}'
    appName: 'web-styleverse-cus-${uniqueString(resourceGroup().id)}'
  }
  {
    location: 'eastasia'
    displayName: 'East Asia'
    planName: 'asp-styleverse-eas-${uniqueString(resourceGroup().id)}'
    appName: 'web-styleverse-eas-${uniqueString(resourceGroup().id)}'
  }
]

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
    restrictOutboundNetworkAccess: 'Disabled'
  }
}

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  name: 'AllowAllWindowsAzureIps'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648
    zoneRedundant: false
    readScale: 'Disabled'
  }
}

module webApps 'modules/webapp.bicep' = [for r in webRegions: {
  name: 'webapp-${r.location}'
  params: {
    location: r.location
    regionDisplayName: r.displayName
    appServicePlanName: r.planName
    webAppName: r.appName
    cosmosEndpoint: 'https://${cosmosAccountName}.documents.azure.com:443/'
    appInsightsConnectionString: appInsights.properties.ConnectionString
    logAnalyticsWorkspaceId: logAnalytics.id
  }
}]

output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name
output webAppNames array = [for (r, i) in webRegions: webApps[i].outputs.name]
output webAppHostNames array = [for (r, i) in webRegions: webApps[i].outputs.hostName]
