# Expense Management System

![Header image](https://github.com/DougChisholm/App-Mod-Assist/blob/main/repo-header.png)

A modern, cloud-native expense management system built on Azure with AI-powered chat capabilities.

## Features

### Core Functionality
- ✅ **Add Expenses** - Create expense entries with categories, amounts, and descriptions
- ✅ **View Expenses** - List all expenses with filtering by status and search
- ✅ **Approve/Reject** - Manager workflow for expense approval
- ✅ **REST APIs** - Complete CRUD operations with Swagger documentation
- ✅ **AI Chat Assistant** - Natural language interface powered by GPT-4o

### Modern Architecture
- **ASP.NET Core 8** (LTS) - Latest .NET framework
- **Azure SQL Database** - With Entra ID-only authentication
- **Managed Identity** - Secure, passwordless authentication
- **Stored Procedures** - All data access through database procedures
- **Azure OpenAI (GPT-4o)** - Advanced AI chat with function calling
- **Azure AI Search** - RAG pattern for contextual responses

### Security & Best Practices
- 🔒 Entra ID-only authentication (no SQL passwords)
- 🔑 Managed Identity for all service connections
- 🛡️ No secrets in code or configuration
- ✅ Comprehensive error handling with detailed diagnostics
- 📊 Structured logging

## Quick Start

### Prerequisites
- Azure CLI installed
- Azure subscription
- Logged in with `az login`

### Deployment

#### Option 1: Basic Deployment (No AI)
```bash
./deploy.sh
```

This deploys:
- App Service (S1 SKU)
- Azure SQL Database
- REST APIs with Swagger
- Web UI for expense management

#### Option 2: Full Deployment (With AI Chat)
```bash
./deploy-with-chat.sh
```

This deploys everything from Option 1 PLUS:
- Azure OpenAI (GPT-4o)
- Azure AI Search
- AI Chat Assistant

### Access the Application

After deployment completes, access:
- **Web UI**: `https://{app-service-name}.azurewebsites.net/Index`
- **AI Chat**: `https://{app-service-name}.azurewebsites.net/Chat`
- **API Docs**: `https://{app-service-name}.azurewebsites.net/swagger`

## Local Development

1. Clone the repository
2. Open in VS Code or Visual Studio
3. Update `appsettings.json` connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=tcp:{your-server}.database.windows.net,1433;Database=ExpenseDB;Authentication=Active Directory Default;Encrypt=True;"
   }
   ```
4. Run `az login`
5. Start the application:
   ```bash
   cd app/ExpenseManagement
   dotnet run
   ```

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture diagram and component descriptions.

### Key Components

```
User Interface (Razor Pages)
    ↓
REST APIs (Controllers)
    ↓
Business Logic (Services)
    ↓
Stored Procedures
    ↓
Azure SQL Database
```

### AI Chat Flow

```
User → Chat UI → ChatService → Azure OpenAI (GPT-4o)
                                     ↓
                               Function Calling
                                     ↓
                               ExpenseService → SQL Database
```

## Project Structure

```
├── infrastructure/          # Bicep templates for Azure resources
│   ├── main.bicep          # Main orchestration template
│   ├── app-service.bicep   # App Service and Managed Identity
│   ├── azure-sql.bicep     # SQL Database configuration
│   └── genai.bicep         # OpenAI and AI Search resources
├── app/ExpenseManagement/   # ASP.NET Core application
│   ├── Controllers/        # API endpoints
│   ├── Services/           # Business logic
│   ├── Models/             # Data models
│   └── Pages/              # Razor Pages UI
├── Database-Schema/        # SQL schema and seed data
├── deploy.sh               # Basic deployment script
├── deploy-with-chat.sh     # Full deployment with AI
├── run-sql.py              # Schema import script
├── run-sql-dbrole.py       # Database role configuration
├── run-sql-stored-procs.py # Stored procedures deployment
├── stored-procedures.sql   # All database stored procedures
└── app.zip                 # Deployment package
```

## API Endpoints

### Expenses
- `GET /api/expenses` - Get all expenses
- `GET /api/expenses/{id}` - Get expense by ID
- `GET /api/expenses/status/{status}` - Get expenses by status
- `POST /api/expenses` - Create new expense
- `PUT /api/expenses/{id}` - Update expense
- `POST /api/expenses/{id}/submit` - Submit for approval
- `POST /api/expenses/{id}/approve` - Approve expense
- `POST /api/expenses/{id}/reject` - Reject expense
- `DELETE /api/expenses/{id}` - Delete expense

### Reference Data
- `GET /api/categories` - Get all categories
- `GET /api/statuses` - Get all statuses
- `GET /api/users` - Get all users

### AI Chat
- `POST /api/chat` - Send message to AI assistant

## Database Schema

### Tables
- **Users** - System users (employees and managers)
- **Roles** - User roles (Employee, Manager)
- **ExpenseCategories** - Expense categories (Travel, Meals, etc.)
- **ExpenseStatus** - Status tracking (Draft, Submitted, Approved, Rejected)
- **Expenses** - Main expense records

### Stored Procedures
All data access is performed through stored procedures:
- `sp_GetAllExpenses` - Retrieve all expenses
- `sp_GetExpensesByStatus` - Filter by status
- `sp_CreateExpense` - Create new expense
- `sp_UpdateExpense` - Update existing expense
- `sp_SubmitExpense` - Submit for approval
- `sp_ApproveExpense` - Approve expense
- `sp_RejectExpense` - Reject expense
- `sp_DeleteExpense` - Delete expense
- Plus category, status, and user procedures

## AI Chat Capabilities

The AI chat assistant can:
- 📊 Query expenses with natural language
- ➕ Create new expenses
- ✅ Submit, approve, or reject expenses
- 📋 List categories and users
- 💡 Provide expense insights and summaries

Example queries:
- "Show me all submitted expenses"
- "Create a travel expense for £50 on 2025-11-20"
- "What are the pending expenses for approval?"
- "Approve expense ID 5"

## Error Handling

The application includes comprehensive error handling:
- Database connection failures display detailed error messages
- Managed Identity issues are diagnosed with specific fixes
- Fallback to dummy data when database is unavailable
- All errors logged with file names and line numbers

## Development Notes

### Building the Application
```bash
cd app/ExpenseManagement
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Creating Deployment Package
```bash
dotnet publish -c Release -o publish
cd publish && zip -r ../../app.zip .
```

## Troubleshooting

### Database Connection Issues
If you see managed identity errors, ensure:
1. Managed identity is created and assigned to App Service
2. Managed identity has db_datareader, db_datawriter, and execute permissions
3. Entra ID admin is configured on SQL Server

### Local Development
For local development, use `Authentication=Active Directory Default` in connection string and ensure you're logged in with `az login`.

### AI Chat Not Working
If AI chat shows a warning message:
1. Ensure you used `deploy-with-chat.sh` for deployment
2. Check that OpenAI settings are configured in App Service
3. Verify managed identity has Cognitive Services OpenAI User role

## Contributing

This project demonstrates modern Azure development practices:
- Infrastructure as Code with Bicep
- Passwordless authentication
- AI-powered experiences
- Clean architecture
- Comprehensive error handling

## License

MIT License - See LICENSE file for details

## Support

For issues or questions, please create an issue in the repository.

---

**Built with ❤️ using Azure, .NET 8, and GPT-4o**
