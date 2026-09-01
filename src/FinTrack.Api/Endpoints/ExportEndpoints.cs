using System.Security.Claims;
using System.Text;
using FinTrack.Api.Auth;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Endpoints;

public static class ExportEndpoints
{
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/exports").WithTags("Exports").RequireAuthorization();

        group.MapGet("/transactions.csv", async (
            int? year,
            int? month,
            DateOnly? startDate,
            DateOnly? endDate,
            TransactionType? type,
            Guid? accountId,
            Guid? categoryId,
            FinTrackDbContext db,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            if (month is < 1 or > 12)
            {
                return Results.BadRequest("Month must be between 1 and 12.");
            }

            if (startDate > endDate)
            {
                return Results.BadRequest("Start date must be before end date.");
            }

            var userId = user.GetUserId();
            var query = db.Transactions.Where(transaction => transaction.UserId == userId);

            if (year is not null)
            {
                query = query.Where(transaction => transaction.Date.Year == year);
            }

            if (month is not null)
            {
                query = query.Where(transaction => transaction.Date.Month == month);
            }

            if (startDate is not null)
            {
                query = query.Where(transaction => transaction.Date >= startDate);
            }

            if (endDate is not null)
            {
                query = query.Where(transaction => transaction.Date <= endDate);
            }

            if (type is not null)
            {
                query = query.Where(transaction => transaction.Type == type);
            }

            if (accountId is not null)
            {
                query = query.Where(transaction => transaction.AccountId == accountId);
            }

            if (categoryId is not null)
            {
                query = query.Where(transaction => transaction.CategoryId == categoryId);
            }

            var transactions = await query
                .OrderByDescending(transaction => transaction.Date)
                .Select(transaction => new
                {
                    transaction.Date,
                    transaction.Type,
                    transaction.Amount,
                    transaction.Description
                })
                .ToListAsync(cancellationToken);

            var csv = new StringBuilder();
            csv.AppendLine("Date,Type,Amount,Description");

            foreach (var transaction in transactions)
            {
                csv
                    .Append(transaction.Date.ToString("yyyy-MM-dd")).Append(',')
                    .Append(transaction.Type).Append(',')
                    .Append(transaction.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                    .Append(Escape(transaction.Description))
                    .AppendLine();
            }

            return Results.File(
                Encoding.UTF8.GetBytes(csv.ToString()),
                "text/csv; charset=utf-8",
                $"fintrack-transactions-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        });

        return app;
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
