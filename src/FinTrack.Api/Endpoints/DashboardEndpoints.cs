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
            if (year is < 1 or > 9999)
            {
                return Results.BadRequest("Year must be between 1 and 9999.");
            }

            if (month is < 1 or > 12)
            {
                return Results.BadRequest("Month must be between 1 and 12.");
            }

            var userId = user.GetUserId();
            var (periodStart, periodEnd) = DateRanges.Month(year, month);

            var monthTransactions = db.Transactions
                .Where(transaction =>
                    transaction.UserId == userId &&
                    transaction.Date >= periodStart &&
                    transaction.Date <= periodEnd);

            var totals = await db.Transactions
                .Where(transaction => transaction.UserId == userId && transaction.Date <= periodEnd)
                .GroupBy(transaction => transaction.Type)
                .Select(group => new
                {
                    Type = group.Key,
                    Current = group.Sum(transaction => transaction.Amount),
                    Monthly = group.Sum(transaction => transaction.Date >= periodStart ? transaction.Amount : 0)
                })
                .ToListAsync(cancellationToken);
            var income = totals.SingleOrDefault(total => total.Type == TransactionType.Income);
            var expense = totals.SingleOrDefault(total => total.Type == TransactionType.Expense);
            var totalIncome = income?.Monthly ?? 0;
            var totalExpense = expense?.Monthly ?? 0;
            var initialBalance = await db.Accounts
                .Where(account => account.UserId == userId && account.OpeningDate <= periodEnd)
                .SumAsync(account => (decimal?)account.InitialBalance, cancellationToken) ?? 0;

            var categoryTotals = await monthTransactions
                .Where(transaction => transaction.Type == TransactionType.Expense)
                .GroupBy(transaction => transaction.CategoryId)
                .Select(group => new { CategoryId = group.Key, Total = group.Sum(transaction => transaction.Amount) })
                .OrderByDescending(total => total.Total)
                .ToListAsync(cancellationToken);
            // Buscar nomes separadamente evita o plano lento do join em históricos grandes.
            var categories = await db.Categories
                .Where(category => category.UserId == userId)
                .Select(category => new { category.Id, category.Name })
                .ToListAsync(cancellationToken);

            var expensesByCategory = categoryTotals
                .Join(
                    categories,
                    total => total.CategoryId,
                    category => category.Id,
                    (total, category) => new CategoryExpenseResponse(category.Id, category.Name, total.Total))
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
                initialBalance + (income?.Current ?? 0) - (expense?.Current ?? 0),
                expensesByCategory,
                latestTransactions));
        });

        return app;
    }
}
