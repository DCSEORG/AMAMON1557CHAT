using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly ILogger<AddExpenseModel> _logger;
    private readonly ExpenseService _expenseService;
    
    public List<User> Users { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public AddExpenseModel(ILogger<AddExpenseModel> logger, ExpenseService expenseService)
    {
        _logger = logger;
        _expenseService = expenseService;
    }

    public async Task OnGetAsync()
    {
        try
        {
            Users = await _expenseService.GetAllUsersAsync();
            Categories = await _expenseService.GetAllCategoriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading form data");
            ErrorMessage = ex.Message;
        }
    }

    public async Task<IActionResult> OnPostAsync(int userId, decimal amount, DateTime expenseDate, 
                                                   int categoryId, string? description)
    {
        try
        {
            var request = new CreateExpenseRequest
            {
                UserId = userId,
                Amount = amount,
                ExpenseDate = expenseDate,
                CategoryId = categoryId,
                Description = description,
                Currency = "GBP"
            };

            await _expenseService.CreateExpenseAsync(request);
            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ErrorMessage = ex.Message;
            await OnGetAsync();
            return Page();
        }
    }
}
