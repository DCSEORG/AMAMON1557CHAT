using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Models;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace ExpenseManagement.Services;

public class ChatService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;
    private readonly ExpenseService _expenseService;
    private ChatClient? _chatClient;
    private string? _deploymentName;

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger, ExpenseService expenseService)
    {
        _configuration = configuration;
        _logger = logger;
        _expenseService = expenseService;
        InitializeClient();
    }

    private void InitializeClient()
    {
        var endpoint = _configuration["OpenAI:Endpoint"];
        _deploymentName = _configuration["OpenAI:DeploymentName"];
        
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(_deploymentName))
        {
            _logger.LogWarning("OpenAI configuration not found. Chat functionality will return dummy responses.");
            return;
        }

        try
        {
            var credential = new DefaultAzureCredential();
            var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
            _chatClient = azureClient.GetChatClient(_deploymentName);
            _logger.LogInformation("ChatService initialized successfully with deployment: {DeploymentName}", _deploymentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize ChatService");
        }
    }

    public async Task<string> ChatAsync(string userMessage, List<ChatMessage> conversationHistory)
    {
        if (_chatClient == null || string.IsNullOrEmpty(_deploymentName))
        {
            return "⚠️ GenAI services are not deployed. To enable AI chat functionality, please run the deploy-with-chat.sh script to deploy Azure OpenAI resources.";
        }

        try
        {
            // Build message list
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(GetSystemPrompt())
            };
            
            messages.AddRange(conversationHistory);
            messages.Add(new UserChatMessage(userMessage));

            // Define function tools
            var tools = GetFunctionTools();
            
            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            // Make the initial chat request
            ClientResult<ChatCompletion> result = await _chatClient.CompleteChatAsync(messages, options);
            var completion = result.Value;

            // Check if the model wants to call functions
            while (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                // Add assistant message with tool calls to history
                messages.Add(new AssistantChatMessage(completion));

                // Execute each tool call
                foreach (var toolCall in completion.ToolCalls)
                {
                    if (toolCall.Kind == ChatToolCallKind.Function)
                    {
                        var functionCall = (ChatToolCall)toolCall;
                        var functionResult = await ExecuteFunctionAsync(functionCall.FunctionName, functionCall.FunctionArguments);
                        messages.Add(new ToolChatMessage(functionCall.Id, functionResult));
                    }
                }

                // Make another request with function results
                result = await _chatClient.CompleteChatAsync(messages, options);
                completion = result.Value;
            }

            return completion.Content[0].Text ?? "I apologize, but I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChatAsync");
            return $"Error: {ex.Message}";
        }
    }

    private string GetSystemPrompt()
    {
        return @"You are an AI assistant for an Expense Management System. You have access to real functions to interact with the expense database.

Available capabilities:
- get_all_expenses: Retrieve all expenses from the system
- get_expenses_by_status: Get expenses filtered by status (Draft, Submitted, Approved, Rejected)
- get_expense_by_id: Get details of a specific expense
- create_expense: Create a new expense entry
- submit_expense: Submit a draft expense for approval
- approve_expense: Approve a submitted expense
- reject_expense: Reject a submitted expense
- delete_expense: Delete an expense
- get_categories: List all expense categories
- get_users: List all system users

When users ask about expenses, use these functions to retrieve actual data from the database.
Be helpful, concise, and professional. Format currency amounts in GBP with proper formatting.
When presenting tabular data, use clear formatting with headers and aligned columns.";
    }

    private List<ChatTool> GetFunctionTools()
    {
        return new List<ChatTool>
        {
            ChatTool.CreateFunctionTool(
                "get_all_expenses",
                "Retrieves all expenses from the database",
                BinaryData.FromString(JsonSerializer.Serialize(new { type = "object", properties = new { } }))
            ),
            ChatTool.CreateFunctionTool(
                "get_expenses_by_status",
                "Get expenses filtered by status",
                BinaryData.FromString(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        status = new
                        {
                            type = "string",
                            description = "The status to filter by",
                            @enum = new[] { "Draft", "Submitted", "Approved", "Rejected" }
                        }
                    },
                    required = new[] { "status" }
                }))
            ),
            ChatTool.CreateFunctionTool(
                "get_expense_by_id",
                "Get details of a specific expense",
                BinaryData.FromString(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        expense_id = new { type = "integer", description = "The ID of the expense" }
                    },
                    required = new[] { "expense_id" }
                }))
            ),
            ChatTool.CreateFunctionTool(
                "create_expense",
                "Create a new expense in the database",
                BinaryData.FromString(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        user_id = new { type = "integer", description = "User ID who created the expense" },
                        category_id = new { type = "integer", description = "Category ID for the expense" },
                        amount = new { type = "number", description = "Amount in GBP" },
                        expense_date = new { type = "string", description = "Date of expense (YYYY-MM-DD)" },
                        description = new { type = "string", description = "Description of the expense" }
                    },
                    required = new[] { "user_id", "category_id", "amount", "expense_date" }
                }))
            ),
            ChatTool.CreateFunctionTool(
                "submit_expense",
                "Submit a draft expense for approval",
                BinaryData.FromString(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        expense_id = new { type = "integer", description = "The ID of the expense to submit" }
                    },
                    required = new[] { "expense_id" }
                }))
            ),
            ChatTool.CreateFunctionTool(
                "approve_expense",
                "Approve a submitted expense",
                BinaryData.FromString(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        expense_id = new { type = "integer", description = "The ID of the expense to approve" },
                        reviewer_id = new { type = "integer", description = "The ID of the user approving" }
                    },
                    required = new[] { "expense_id", "reviewer_id" }
                }))
            ),
            ChatTool.CreateFunctionTool(
                "reject_expense",
                "Reject a submitted expense",
                BinaryData.FromString(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = new
                    {
                        expense_id = new { type = "integer", description = "The ID of the expense to reject" },
                        reviewer_id = new { type = "integer", description = "The ID of the user rejecting" }
                    },
                    required = new[] { "expense_id", "reviewer_id" }
                }))
            ),
            ChatTool.CreateFunctionTool(
                "get_categories",
                "List all expense categories",
                BinaryData.FromString(JsonSerializer.Serialize(new { type = "object", properties = new { } }))
            ),
            ChatTool.CreateFunctionTool(
                "get_users",
                "List all users in the system",
                BinaryData.FromString(JsonSerializer.Serialize(new { type = "object", properties = new { } }))
            )
        };
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, BinaryData argumentsData)
    {
        try
        {
            var arguments = JsonDocument.Parse(argumentsData);
            
            return functionName switch
            {
                "get_all_expenses" => await GetAllExpensesFunction(),
                "get_expenses_by_status" => await GetExpensesByStatusFunction(arguments),
                "get_expense_by_id" => await GetExpenseByIdFunction(arguments),
                "create_expense" => await CreateExpenseFunction(arguments),
                "submit_expense" => await SubmitExpenseFunction(arguments),
                "approve_expense" => await ApproveExpenseFunction(arguments),
                "reject_expense" => await RejectExpenseFunction(arguments),
                "get_categories" => await GetCategoriesFunction(),
                "get_users" => await GetUsersFunction(),
                _ => JsonSerializer.Serialize(new { error = "Unknown function" })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private async Task<string> GetAllExpensesFunction()
    {
        var expenses = await _expenseService.GetAllExpensesAsync();
        return JsonSerializer.Serialize(expenses);
    }

    private async Task<string> GetExpensesByStatusFunction(JsonDocument arguments)
    {
        var status = arguments.RootElement.GetProperty("status").GetString() ?? "Submitted";
        var expenses = await _expenseService.GetExpensesByStatusAsync(status);
        return JsonSerializer.Serialize(expenses);
    }

    private async Task<string> GetExpenseByIdFunction(JsonDocument arguments)
    {
        var expenseId = arguments.RootElement.GetProperty("expense_id").GetInt32();
        var expense = await _expenseService.GetExpenseByIdAsync(expenseId);
        return JsonSerializer.Serialize(expense);
    }

    private async Task<string> CreateExpenseFunction(JsonDocument arguments)
    {
        var root = arguments.RootElement;
        var request = new CreateExpenseRequest
        {
            UserId = root.GetProperty("user_id").GetInt32(),
            CategoryId = root.GetProperty("category_id").GetInt32(),
            Amount = root.GetProperty("amount").GetDecimal(),
            ExpenseDate = DateTime.Parse(root.GetProperty("expense_date").GetString()!),
            Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Currency = "GBP"
        };
        
        var expenseId = await _expenseService.CreateExpenseAsync(request);
        return JsonSerializer.Serialize(new { success = true, expense_id = expenseId });
    }

    private async Task<string> SubmitExpenseFunction(JsonDocument arguments)
    {
        var expenseId = arguments.RootElement.GetProperty("expense_id").GetInt32();
        var result = await _expenseService.SubmitExpenseAsync(expenseId);
        return JsonSerializer.Serialize(new { success = result > 0 });
    }

    private async Task<string> ApproveExpenseFunction(JsonDocument arguments)
    {
        var root = arguments.RootElement;
        var expenseId = root.GetProperty("expense_id").GetInt32();
        var reviewerId = root.GetProperty("reviewer_id").GetInt32();
        var result = await _expenseService.ApproveExpenseAsync(expenseId, reviewerId);
        return JsonSerializer.Serialize(new { success = result > 0 });
    }

    private async Task<string> RejectExpenseFunction(JsonDocument arguments)
    {
        var root = arguments.RootElement;
        var expenseId = root.GetProperty("expense_id").GetInt32();
        var reviewerId = root.GetProperty("reviewer_id").GetInt32();
        var result = await _expenseService.RejectExpenseAsync(expenseId, reviewerId);
        return JsonSerializer.Serialize(new { success = result > 0 });
    }

    private async Task<string> GetCategoriesFunction()
    {
        var categories = await _expenseService.GetAllCategoriesAsync();
        return JsonSerializer.Serialize(categories);
    }

    private async Task<string> GetUsersFunction()
    {
        var users = await _expenseService.GetAllUsersAsync();
        return JsonSerializer.Serialize(users);
    }
}
