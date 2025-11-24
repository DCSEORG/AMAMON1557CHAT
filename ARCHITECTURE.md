# Azure Services Architecture

## Expense Management System - Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Azure Resource Group                            │
│                         (rg-expensemgmt)                                 │
└─────────────────────────────────────────────────────────────────────────┘
                                     │
            ┌────────────────────────┼────────────────────────┐
            │                        │                        │
            ▼                        ▼                        ▼
┌───────────────────────┐ ┌──────────────────────┐ ┌──────────────────────┐
│   App Service (S1)    │ │  Azure SQL Database  │ │ User-Assigned        │
│   .NET 8 Web App      │ │  (Basic Tier)        │ │ Managed Identity     │
│                       │ │                      │ │                      │
│ - Razor Pages UI      │ │ - ExpenseDB          │ │ Authenticates to:    │
│ - REST APIs           │ │ - Entra ID Auth Only │ │ - SQL Database       │
│ - Swagger Docs        │ │ - Stored Procedures  │ │ - Azure OpenAI       │
│ - Chat UI             │ │                      │ │ - AI Search          │
└───────────┬───────────┘ └──────────┬───────────┘ └──────────┬───────────┘
            │                        │                        │
            │   ┌────────────────────┘                        │
            │   │                                             │
            │   │ Managed Identity Auth                       │
            │   └─────────────────────────────────────────────┘
            │
            │   (When deploy-with-chat.sh is used)
            │
            ├─────────────────────────────────────────────────────┐
            │                                                     │
            ▼                                                     ▼
┌───────────────────────┐                           ┌──────────────────────┐
│  Azure OpenAI (S0)    │                           │  Azure AI Search     │
│  (Sweden Central)     │                           │  (Basic)             │
│                       │                           │                      │
│ - GPT-4o Model        │                           │ - RAG Pattern        │
│ - Function Calling    │                           │ - Document Index     │
│ - Chat Completion     │                           │                      │
└───────────────────────┘                           └──────────────────────┘


┌─────────────────────────────────────────────────────────────────────────┐
│                          Data Flow                                       │
└─────────────────────────────────────────────────────────────────────────┘

User → App Service UI → REST API → Stored Procedures → SQL Database
User → Chat UI → ChatService → Azure OpenAI (Function Calling) → ExpenseService → SQL Database
                                     ↓
                               AI Search (RAG)


┌─────────────────────────────────────────────────────────────────────────┐
│                          Authentication                                  │
└─────────────────────────────────────────────────────────────────────────┘

Deployment User ←──┐
                   │ Entra ID Admin
                   ▼
              SQL Server ←── Managed Identity (db_datareader, db_datawriter, execute)
                   ▲
                   │
              App Service (Managed Identity)
                   │
                   ├─→ Azure OpenAI (Cognitive Services OpenAI User role)
                   └─→ AI Search (Search Index Data Contributor role)


┌─────────────────────────────────────────────────────────────────────────┐
│                          Deployment Sequence                             │
└─────────────────────────────────────────────────────────────────────────┘

1. deploy.sh (Basic deployment)
   - Resource Group
   - User-Assigned Managed Identity
   - App Service + App Service Plan
   - SQL Server + Database
   - SQL Firewall Rules
   - Database Schema Import
   - Database Role Configuration
   - Stored Procedures Deployment
   - Application Code Deployment

2. deploy-with-chat.sh (Full deployment with AI)
   - All of deploy.sh steps PLUS:
   - Azure OpenAI (GPT-4o in Sweden)
   - Azure AI Search
   - Role Assignments for Managed Identity
   - App Service Settings Configuration
   - OpenAI Endpoint Configuration
   - Search Endpoint Configuration
```

## Key Features

### Security
- **Entra ID-Only Authentication** for SQL Database (no SQL auth)
- **Managed Identity** for all service-to-service authentication
- **No secrets or connection strings** in code
- **HTTPS only** for all communications

### High Availability
- **App Service S1** tier (no cold start)
- **Connection pooling** for database
- **Error handling** with fallback to dummy data

### Modern Architecture
- **ASP.NET Core 8** (LTS)
- **Razor Pages** for UI
- **REST APIs** with Swagger documentation
- **AI Chat** with GPT-4o function calling
- **RAG Pattern** for contextual AI responses
- **Stored Procedures** for all data access (no inline SQL)

### Deployment Options

1. **Basic Deployment** (`deploy.sh`)
   - App Service, SQL Database, APIs
   - No AI features
   - Chat UI shows message to deploy GenAI services

2. **Full Deployment** (`deploy-with-chat.sh`)
   - Everything in basic deployment
   - Azure OpenAI with GPT-4o
   - Azure AI Search
   - Fully functional AI chat assistant

## URLs

After deployment:
- **Application**: `https://{app-service-name}.azurewebsites.net/Index`
- **Chat UI**: `https://{app-service-name}.azurewebsites.net/Chat`
- **API Docs**: `https://{app-service-name}.azurewebsites.net/swagger`
