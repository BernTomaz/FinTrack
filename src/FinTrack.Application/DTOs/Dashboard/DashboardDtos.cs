namespace FinTrack.Application.DTOs.Dashboard;

public sealed record MonthlyDashboardResponse(
    int Year,
    int Month,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal MonthBalance,
    decimal CurrentBalance,
    IReadOnlyList<CategoryExpenseResponse> ExpensesByCategory,
    IReadOnlyList<LatestTransactionResponse> LatestTransactions);

public sealed record CategoryExpenseResponse(Guid CategoryId, string CategoryName, decimal Total);

public sealed record LatestTransactionResponse(
    Guid Id,
    string Type,
    decimal Amount,
    DateOnly Date,
    string? Description);
