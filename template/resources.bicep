param staticWebAppName string
param repositoryUrl string
param branch string
param apiLocation string
param appArtifactLocation string
param appLocation string

@secure()
param repositoryToken string
param functionName string
param storageAccountName string
param accountType string
param kind string
param accessTier string
param minimumTlsVersion string
param supportsHttpsTrafficOnly bool
param use32BitWorkerProcess bool
param sku string
param skuCode string
param workerSize string
param workerSizeId string
param numberOfWorkers string
param profileName string
param endpointName string
param CDNSku object
param profileProperties object
param endpointProperties object

var hostingPlanName_var = replace(functionName, 'func', 'plan')
var applicationInsightsName_var = replace(functionName, 'func', 'appi')

resource profileName_resource 'microsoft.cdn/profiles@2020-04-15' = {
  name: profileName
  location: 'Global'
  sku: CDNSku
  properties: profileProperties
}

resource profileName_endpointName 'microsoft.cdn/profiles/endpoints@2020-04-15' = {
  name: '${profileName_resource.name}/${endpointName}'
  location: 'Global'
  properties: endpointProperties
}

resource staticWebAppName_resource 'Microsoft.Web/staticSites@2019-12-01-preview' = {
  name: staticWebAppName
  location: resourceGroup().location
  tags: {}
  properties: {
    repositoryUrl: repositoryUrl
    branch: branch
    repositoryToken: repositoryToken
    buildProperties: {
      appLocation: appLocation
      apiLocation: apiLocation
      appArtifactLocation: appArtifactLocation
    }
  }
  sku: {
    Tier: 'Free'
    Name: 'Free'
  }
}

resource applicationInsightsName 'microsoft.insights/components@2015-05-01' = {
  name: applicationInsightsName_var
  location: 'westeurope'
  tags: {}
  kind: 'web'
}

resource storageAccountName_resource 'Microsoft.Storage/storageAccounts@2019-06-01' = {
  name: storageAccountName
  location: resourceGroup().location
  properties: {
    accessTier: accessTier
    minimumTlsVersion: minimumTlsVersion
    supportsHttpsTrafficOnly: supportsHttpsTrafficOnly
  }
  sku: {
    name: accountType
  }
  kind: kind
  tags: {}
  dependsOn: []
}

resource storageAccountName_default_shorturls 'Microsoft.Storage/storageAccounts/tableServices/tables@2019-06-01' = {
  name: '${storageAccountName}/default/shorturls'
  dependsOn: [
    storageAccountName_resource
  ]
}

resource storageAccountName_default_configuration 'Microsoft.Storage/storageAccounts/tableServices/tables@2019-06-01' = {
  name: '${storageAccountName}/default/configuration'
  dependsOn: [
    storageAccountName_resource
  ]
}

resource storageAccountName_default_users 'Microsoft.Storage/storageAccounts/tableServices/tables@2019-06-01' = {
  name: '${storageAccountName}/default/users'
  dependsOn: [
    storageAccountName_resource
  ]
}

resource functionName_resource 'Microsoft.Web/sites@2018-11-01' = {
  name: functionName
  kind: 'functionapp'
  location: resourceGroup().location
  tags: {}
  properties: {
    name: functionName
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'node'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~12'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: reference(applicationInsightsName.id, '2015-05-01').InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: reference(applicationInsightsName.id, '2015-05-01').ConnectionString
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listKeys(storageAccountName_resource.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listKeys(storageAccountName_resource.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'AzureStorageConnection'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${listKeys(storageAccountName_resource.id, '2019-06-01').keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionName)
        }
      ]
      use32BitWorkerProcess: use32BitWorkerProcess
    }
    serverFarmId: hostingPlanName.id
    clientAffinityEnabled: false
  }
}

resource functionName_web 'Microsoft.Web/sites/config@2016-08-01' = {
  name: '${functionName_resource.name}/web'
  properties: {
    cors: {
      allowedOrigins: [
        'https://functions.azure.com'
        'https://functions-staging.azure.com'
        'https://functions-next.azure.com'
        'https://shorter.apps.hueppauff.com'
        'https://localhost:5001'
      ]
    }
  }
}

resource hostingPlanName 'Microsoft.Web/serverfarms@2018-11-01' = {
  name: hostingPlanName_var
  location: resourceGroup().location
  kind: ''
  tags: {}
  properties: {
    name: hostingPlanName_var
    workerSize: workerSize
    workerSizeId: workerSizeId
    numberOfWorkers: numberOfWorkers
  }
  sku: {
    Tier: sku
    Name: skuCode
  }
  dependsOn: []
}
