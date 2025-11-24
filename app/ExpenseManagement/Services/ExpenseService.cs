using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using System.Data;

namespace ExpenseManagement.Services;

public class ExpenseService
{
    private readonly string _connectionString;
    private readonly ILogger<ExpenseService> _logger;
    private const string ERROR_PREFIX = "[ExpenseService Error]";

    public ExpenseService(IConfiguration configuration, ILogger<ExpenseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection not configured");
        _logger = logger;
    }

    public async Task<List<Expense>> GetAllExpensesAsync()
    {
        try
        {
            var expenses = new List<Expense>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_GetAllExpenses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in GetAllExpensesAsync at line 38");
            throw new Exception($"{ERROR_PREFIX} Database connection failed. Check managed identity permissions. File: ExpenseService.cs, Line: 38", ex);
        }
    }

    public async Task<List<Expense>> GetExpensesByStatusAsync(string statusName)
    {
        try
        {
            var expenses = new List<Expense>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_GetExpensesByStatus", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@StatusName", statusName);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in GetExpensesByStatusAsync at line 66");
            throw new Exception($"{ERROR_PREFIX} Database query failed. File: ExpenseService.cs, Line: 66", ex);
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_GetExpenseById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapExpense(reader);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in GetExpenseByIdAsync at line 93");
            throw new Exception($"{ERROR_PREFIX} Database query failed. File: ExpenseService.cs, Line: 93", ex);
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_CreateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@UserId", request.UserId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@Currency", request.Currency);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in CreateExpenseAsync at line 124");
            throw new Exception($"{ERROR_PREFIX} Failed to create expense. File: ExpenseService.cs, Line: 124", ex);
        }
    }

    public async Task<int> UpdateExpenseAsync(UpdateExpenseRequest request)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_UpdateExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            command.Parameters.AddWithValue("@ExpenseId", request.ExpenseId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@Currency", request.Currency);
            command.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate);
            command.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@ReceiptFile", (object?)request.ReceiptFile ?? DBNull.Value);
            
            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in UpdateExpenseAsync at line 153");
            throw new Exception($"{ERROR_PREFIX} Failed to update expense. File: ExpenseService.cs, Line: 153", ex);
        }
    }

    public async Task<int> SubmitExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_SubmitExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in SubmitExpenseAsync at line 174");
            throw new Exception($"{ERROR_PREFIX} Failed to submit expense. File: ExpenseService.cs, Line: 174", ex);
        }
    }

    public async Task<int> ApproveExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_ApproveExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewedBy", reviewedBy);
            
            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in ApproveExpenseAsync at line 197");
            throw new Exception($"{ERROR_PREFIX} Failed to approve expense. File: ExpenseService.cs, Line: 197", ex);
        }
    }

    public async Task<int> RejectExpenseAsync(int expenseId, int reviewedBy)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_RejectExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@ReviewedBy", reviewedBy);
            
            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in RejectExpenseAsync at line 220");
            throw new Exception($"{ERROR_PREFIX} Failed to reject expense. File: ExpenseService.cs, Line: 220", ex);
        }
    }

    public async Task<int> DeleteExpenseAsync(int expenseId)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_DeleteExpense", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in DeleteExpenseAsync at line 242");
            throw new Exception($"{ERROR_PREFIX} Failed to delete expense. File: ExpenseService.cs, Line: 242", ex);
        }
    }

    public async Task<List<ExpenseCategory>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = new List<ExpenseCategory>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_GetAllCategories", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                });
            }
            
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in GetAllCategoriesAsync at line 273");
            throw new Exception($"{ERROR_PREFIX} Failed to get categories. File: ExpenseService.cs, Line: 273", ex);
        }
    }

    public async Task<List<ExpenseStatus>> GetAllStatusesAsync()
    {
        try
        {
            var statuses = new List<ExpenseStatus>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_GetAllStatuses", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
                    StatusName = reader.GetString(reader.GetOrdinal("StatusName"))
                });
            }
            
            return statuses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in GetAllStatusesAsync at line 304");
            throw new Exception($"{ERROR_PREFIX} Failed to get statuses. File: ExpenseService.cs, Line: 304", ex);
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            var users = new List<User>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            using var command = new SqlCommand("sp_GetAllUsers", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
                    RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                    ManagerId = reader.IsDBNull(reader.GetOrdinal("ManagerId")) ? null : reader.GetInt32(reader.GetOrdinal("ManagerId")),
                    ManagerName = reader.IsDBNull(reader.GetOrdinal("ManagerName")) ? null : reader.GetString(reader.GetOrdinal("ManagerName")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }
            
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{ERROR_PREFIX} Error in GetAllUsersAsync at line 345");
            throw new Exception($"{ERROR_PREFIX} Failed to get users. File: ExpenseService.cs, Line: 345", ex);
        }
    }

    private static Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = reader.GetInt32(reader.GetOrdinal("ExpenseId")),
            UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
            UserName = reader.GetString(reader.GetOrdinal("UserName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
            CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
            StatusId = reader.GetInt32(reader.GetOrdinal("StatusId")),
            StatusName = reader.GetString(reader.GetOrdinal("StatusName")),
            AmountMinor = reader.GetInt32(reader.GetOrdinal("AmountMinor")),
            AmountGBP = reader.GetDecimal(reader.GetOrdinal("AmountGBP")),
            Currency = reader.GetString(reader.GetOrdinal("Currency")),
            ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            ReceiptFile = reader.IsDBNull(reader.GetOrdinal("ReceiptFile")) ? null : reader.GetString(reader.GetOrdinal("ReceiptFile")),
            SubmittedAt = reader.IsDBNull(reader.GetOrdinal("SubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("SubmittedAt")),
            ReviewedBy = reader.IsDBNull(reader.GetOrdinal("ReviewedBy")) ? null : reader.GetInt32(reader.GetOrdinal("ReviewedBy")),
            ReviewedByName = reader.IsDBNull(reader.GetOrdinal("ReviewedByName")) ? null : reader.GetString(reader.GetOrdinal("ReviewedByName")),
            ReviewedAt = reader.IsDBNull(reader.GetOrdinal("ReviewedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ReviewedAt")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
        };
    }

    public List<Expense> GetDummyData()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Demo User",
                Email = "demo@example.com",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 2540,
                AmountGBP = 25.40m,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-5),
                Description = "Taxi from airport",
                CreatedAt = DateTime.Now.AddDays(-5)
            }
        };
    }
}
