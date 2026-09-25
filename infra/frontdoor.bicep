targetScope = 'resourceGroup'

@description('Front Door profile name')
param profileName string = 'afd-styleverse-${uniqueString(resourceGroup().id)}'

@description('Front Door endpoint name (becomes <name>-<hash>.azurefd.net)')
param endpointName string = 'styleverse-${uniqueString(resourceGroup().id)}'

@description('Default host names of the regional web apps')
param originHostNames array = [
  'web-styleverse-${uniqueString(resourceGroup().id)}.azurewebsites.net'
  'web-styleverse-cus-${uniqueString(resourceGroup().id)}.azurewebsites.net'
  'web-styleverse-eas-${uniqueString(resourceGroup().id)}.azurewebsites.net'
]

resource profile 'Microsoft.Cdn/profiles@2024-02-01' = {
  name: profileName
  location: 'global'
  sku: {
    name: 'Standard_AzureFrontDoor'
  }
  properties: {
    originResponseTimeoutSeconds: 60
  }
}

resource endpoint 'Microsoft.Cdn/profiles/afdEndpoints@2024-02-01' = {
  parent: profile
  name: endpointName
  location: 'global'
  properties: {
    enabledState: 'Enabled'
  }
}

resource originGroup 'Microsoft.Cdn/profiles/originGroups@2024-02-01' = {
  parent: profile
  name: 'styleverse-apps'
  properties: {
    // 0 ms tolerance: always send users to the lowest-latency healthy region.
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 0
    }
    healthProbeSettings: {
      probePath: '/api/health'
      probeRequestType: 'GET'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 60
    }
    // Keeps a shopper on one region so their cart reads stay session-consistent.
    sessionAffinityState: 'Enabled'
  }
}

resource origins 'Microsoft.Cdn/profiles/originGroups/origins@2024-02-01' = [for (host, i) in originHostNames: {
  parent: originGroup
  name: 'app-${i}'
  properties: {
    hostName: host
    originHostHeader: host
    httpPort: 80
    httpsPort: 443
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}]

// No caching: API responses are live data.
resource route 'Microsoft.Cdn/profiles/afdEndpoints/routes@2024-02-01' = {
  parent: endpoint
  name: 'default'
  dependsOn: [
    origins
  ]
  properties: {
    originGroup: {
      id: originGroup.id
    }
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
  }
}

// Vite bundles under /assets have content-hashed names, so they're safe to cache at the edge.
resource assetsRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2024-02-01' = {
  parent: endpoint
  name: 'static-assets'
  dependsOn: [
    origins
    route
  ]
  properties: {
    originGroup: {
      id: originGroup.id
    }
    supportedProtocols: [
      'Http'
      'Https'
    ]
    patternsToMatch: [
      '/assets/*'
    ]
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
    cacheConfiguration: {
      queryStringCachingBehavior: 'IgnoreQueryString'
      compressionSettings: {
        isCompressionEnabled: true
        contentTypesToCompress: [
          'application/javascript'
          'text/javascript'
          'text/css'
        ]
      }
    }
  }
}

output frontDoorHostName string = endpoint.properties.hostName
output frontDoorUrl string = 'https://${endpoint.properties.hostName}'
