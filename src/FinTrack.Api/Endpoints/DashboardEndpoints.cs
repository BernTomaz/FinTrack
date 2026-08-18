using System.Security.Claims;
using FinTrack.Api.Auth;
using FinTrack.Application.DTOs.Dashboard;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dashboard").WithTags("Dashboard").RequireAuthorization();

        group.MapGet("/monthly", async (int year, int month, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            if (month is < 1 or > 12)
            {
                return Results.BadRequest("Month must be between 1 and 12.");
            }

            var userId = user.GetUserId();
            var periodStart = new DateOnly(year, month, 1);
            var periodEnd = periodStart.AddMonths(1);

            var monthTransactions = db.Transactions
                .Where(transaction =>
                    transaction.UserId == userId &&
                    transaction.Date >= periodStart &&
                    transaction.Date < periodEnd);

            var totalIncome = await monthTransactions
                .Where(transaction => transaction.Type == TransactionType.Income)
                .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0;
            var totalExpense = await monthTransactions
                .Where(transaction => transaction.Type == TransactionType.Expense)
                .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0;
            var currentIncome = await db.Transactions
                .Where(transaction => transaction.UserId == userId && transaction.Type == TransactionType.Income)
                .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0;
            var currentExpense = await db.Transactions
                .Where(transaction => transaction.UserId == userId && transaction.Type == TransactionType.Expense)
                .SumAsync(transaction => (decimal?)transaction.Amount, cancellationToken) ?? 0;

            var expenseRows = await monthTransactions
                .Where(transaction => transaction.Type == TransactionType.Expense)
                .Join(
                    db.Categories,
                    transaction => transaction.CategoryId,
                    category => category.Id,
                    (transaction, category) => new { transaction, category })
                .ToListAsync(cancellationToken);

            var expensesByCategory = expenseRows
                .GroupBy(item => new { item.category.Id, item.category.Name })
                .Select(group => new CategoryExpenseResponse(
                    group.Key.Id,
                    group.Key.Name,
                    group.Sum(item => item.transaction.Amount)))
                .OrderByDescending(item => item.Total)
                .ToList();

            var latestTransactions = await monthTransactions
                .OrderByDescending(transaction => transaction.Date)
                .ThenByDescending(transaction => transaction.CreatedAt)
                .Take(5)
                .Select(transaction => new LatestTransactionResponse(
                    transaction.Id,
                    transaction.Type.ToString(),
                    transaction.Amount,
                    transaction.Date,
                    transaction.Description))
                .ToListAsync(cancellationToken);

            return Results.Ok(new MonthlyDashboardResponse(
                year,
                month,
                totalIncome,
                totalExpense,
                totalIncome - totalExpense,
                currentIncome - currentExpense,
                expensesByCategory,
                latestTransactions));
        });

        return app;
    }
}
