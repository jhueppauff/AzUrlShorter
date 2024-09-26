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
param endpointName2 string
param CDNSku object
param profileProperties object
param endpointProperties object
param endpointProperties2 object

var hostingPlanName_var = replace(functionName, 'func', 'plan')
var applicationInsightsName_var = replace(functionName, 'func', 'appi')

resource profileName_resource 'microsoft.cdn/profiles@2020-04-15' = {
  name: profileName
  location: 'Global'
  sku: CDNSku
  properties: profileProperties
}

resource profileName_endpointName 'microsoft.cdn/profiles/endpoints@2020-04-15' = {
  parent: profileName_resource
  name: endpointName
  location: 'Global'
  properties: endpointProperties
}

resource profileName_endpointName2 'microsoft.cdn/profiles/endpoints@2020-04-15' = {
  parent: profileName_resource
  name: endpointName2
  location: 'Global'
  properties: endpointProperties2
}

resource staticWebAppName_resource 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: resourceGroup().location
  tags: {}
  properties: {
    repositoryUrl: repositoryUrl
    branch: branch
    repositoryToken: repositoryToken
    provider: 'GitHub'
    buildProperties: {
      appLocation: appLocation
      apiLocation: apiLocation
      appArtifactLocation: appArtifactLocation
    }
  }
  sku: {
    tier: 'Free'
    name: 'Free'
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'logAnalyticsWorkspace'
  location: 'westeurope'
  tags: {}
  properties: {
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource applicationInsightsName 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName_var
  location: 'westeurope'
  tags: {}
  kind: 'web'
  properties: {
    WorkspaceResourceId: logAnalyticsWorkspace.id
    Application_Type: 'web'
  }
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
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
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
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
      use32BitWorkerProcess: use32BitWorkerProcess
    }
    serverFarmId: hostingPlanName.id
    clientAffinityEnabled: false
  }
}

resource functionName_web 'Microsoft.Web/sites/config@2016-08-01' = {
  parent: functionName_resource
  name: 'web'
  properties: {
    cors: {
      allowedOrigins: [
        'https://functions.azure.com'
        'https://functions-staging.azure.com'
        'https://functions-next.azure.com'
        'https://shorter.hueppauff.com'
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
    tier: sku
    name: skuCode
  }
  dependsOn: []
}
