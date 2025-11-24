#!/bin/bash
set -e

echo "=========================================="
echo "Expense Management System - Full Deployment with GenAI"
echo "=========================================="

# Configuration
RESOURCE_GROUP="rg-expensemgmt"
LOCATION="uksouth"

# Get current user information for SQL admin
CURRENT_USER=$(az account show --query user.name -o tsv)
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

echo ""
echo "Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Admin User: $CURRENT_USER"
echo "  Admin Object ID: $ADMIN_OBJECT_ID"
echo ""

# Create resource group
echo "Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION

# Deploy infrastructure WITH GenAI
echo "Deploying infrastructure (App Service, SQL Database, Azure OpenAI, AI Search)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infrastructure/main.bicep \
  --parameters location=$LOCATION \
               adminObjectId=$ADMIN_OBJECT_ID \
               adminLogin=$CURRENT_USER \
               deployGenAI=true \
  --output json)

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceUrl.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.sqlServerName.value')
SQL_DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.sqlDatabaseName.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityClientId.value')
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.searchEndpoint.value')

echo ""
echo "Deployment completed!"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_NAME"
echo "  Database: $SQL_DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "  OpenAI Model: $OPENAI_MODEL_NAME"
echo "  Search Endpoint: $SEARCH_ENDPOINT"
echo ""

# Configure App Service settings with GenAI endpoints
echo "Configuring App Service settings..."
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Database=${SQL_DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
    "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
    "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
    "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
    "Search__Endpoint=$SEARCH_ENDPOINT" \
  --output none

echo "Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30

# Add current user's IP to SQL firewall for schema import
echo "Adding your IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name "AllowLocalIP" \
  --start-ip-address $MY_IP \
  --end-ip-address $MY_IP \
  --output none

# Install Python dependencies
echo "Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual server name
echo "Updating Python scripts with server names..."
sed -i.bak "s/sql-expensemgmt-placeholder/${SQL_SERVER_NAME}/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder/${SQL_SERVER_NAME}/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder/${SQL_SERVER_NAME}/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak

# Update script.sql with managed identity name
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Import database schema
echo "Importing database schema..."
python3 run-sql.py

# Configure database roles for managed identity
echo "Configuring database roles for managed identity..."
python3 run-sql-dbrole.py

# Deploy stored procedures
echo "Deploying stored procedures..."
python3 run-sql-stored-procs.py

# Deploy application code
if [ -f "app.zip" ]; then
    echo "Deploying application code..."
    az webapp deploy \
      --resource-group $RESOURCE_GROUP \
      --name $APP_SERVICE_NAME \
      --src-path app.zip \
      --type zip \
      --output none
    
    echo ""
    echo "=========================================="
    echo "Full Deployment Complete!"
    echo "=========================================="
    echo ""
    echo "Application URL: ${APP_SERVICE_URL}/Index"
    echo "Chat UI URL: ${APP_SERVICE_URL}/Chat"
    echo ""
    echo "GenAI Services Deployed:"
    echo "  - Azure OpenAI (GPT-4o)"
    echo "  - Azure AI Search"
    echo ""
    echo "Note: Navigate to ${APP_SERVICE_URL}/Index to view the application"
    echo "      Navigate to ${APP_SERVICE_URL}/Chat for AI chat interface"
    echo ""
else
    echo ""
    echo "=========================================="
    echo "Infrastructure Deployment Complete!"
    echo "=========================================="
    echo ""
    echo "Application URL: ${APP_SERVICE_URL}/Index"
    echo ""
    echo "Note: app.zip not found. Build the application first with:"
    echo "      cd app && dotnet publish -c Release -o publish"
    echo "      cd publish && zip -r ../../app.zip . && cd ../.."
    echo ""
    echo "Then run this script again to deploy the code."
    echo ""
fi

echo "Local Development:"
echo "  To run locally, update appsettings.json connection string to use:"
echo "  Authentication=Active Directory Default"
echo "  Then run: az login"
echo ""
