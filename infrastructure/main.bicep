// Main Bicep template for Expense Management System
targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = 'uksouth'

@description('Object ID of the Azure AD admin')
param adminObjectId string

@description('Login name of the Azure AD admin')
param adminLogin string

@description('Whether to deploy GenAI resources')
param deployGenAI bool = false

// Generate unique suffix for resource names
var uniqueSuffix = uniqueString(resourceGroup().id)
var appServiceName = 'app-expensemgmt-${uniqueSuffix}'
var sqlServerName = 'sql-expensemgmt-${uniqueSuffix}'
var managedIdentityName = 'mid-expensemgmt-${uniqueSuffix}'

// Deploy App Service with Managed Identity
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    appServiceName: appServiceName
    managedIdentityName: managedIdentityName
  }
}

// Deploy Azure SQL Database
module sqlDatabase 'azure-sql.bicep' = {
  name: 'sqlDatabaseDeployment'
  params: {
    location: location
    sqlServerName: sqlServerName
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Deploy GenAI resources (optional)
module genai 'genai.bicep' = if (deployGenAI) {
  name: 'genaiDeployment'
  params: {
    location: location
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output sqlServerName string = sqlServerName
output sqlDatabaseName string = sqlDatabase.outputs.databaseName
output managedIdentityName string = managedIdentityName
output managedIdentityClientId string = appService.outputs.managedIdentityClientId

// GenAI outputs (conditional)
output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genai.outputs.searchEndpoint : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
