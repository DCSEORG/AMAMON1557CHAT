using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ApproveExpensesModel : PageModel
{
    private readonly ILogger<ApproveExpensesModel> _logger;
    private readonly ExpenseService _expenseService;
    
    public List<Expense> PendingExpenses { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public ApproveExpensesModel(ILogger<ApproveExpensesModel> logger, ExpenseService expenseService)
    {
        _logger = logger;
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        try
        {
            PendingExpenses = await _expenseService.GetExpensesByStatusAsync("Submitted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading pending expenses");
            ErrorMessage = ex.Message;
        }
    }
}
