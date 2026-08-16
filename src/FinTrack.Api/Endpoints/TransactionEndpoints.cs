using System.Security.Claims;
using FinTrack.Api.Auth;
using FinTrack.Application.DTOs.Transactions;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/transactions").WithTags("Transactions").RequireAuthorization();

        group.MapGet("/", async (
            int? year,
            int? month,
            TransactionType? type,
            Guid? accountId,
            Guid? categoryId,
            FinTrackDbContext db,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
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
                .ThenByDescending(transaction => transaction.CreatedAt)
                .Select(transaction => new TransactionResponse(
                    transaction.Id,
                    transaction.AccountId,
                    transaction.CategoryId,
                    transaction.Type,
                    transaction.Amount,
                    transaction.Date,
                    transaction.Description,
                    transaction.CreatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(transactions);
        });

        group.MapGet("/{id:guid}", async (Guid id, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            var transaction = await db.Transactions
                .Where(transaction => transaction.UserId == userId && transaction.Id == id)
                .Select(transaction => new TransactionResponse(
                    transaction.Id,
                    transaction.AccountId,
                    transaction.CategoryId,
                    transaction.Type,
                    transaction.Amount,
                    transaction.Date,
                    transaction.Description,
                    transaction.CreatedAt))
                .SingleOrDefaultAsync(cancellationToken);

            return transaction is null ? Results.NotFound() : Results.Ok(transaction);
        });

        group.MapPost("/", async (TransactionRequest request, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var requestValidation = ValidateRequest(request);
            if (requestValidation is not null)
            {
                return requestValidation;
            }

            var userId = user.GetUserId();
            var transaction = new Transaction(userId, request.AccountId, request.CategoryId, request.Type, request.Amount, request.Date, request.Description);
            var validation = await ValidateReferences(transaction, db, cancellationToken);
            if (validation is not null)
            {
                return validation;
            }

            db.Transactions.Add(transaction);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/transactions/{transaction.Id}", ToResponse(transaction));
        });

        group.MapPut("/{id:guid}", async (Guid id, TransactionRequest request, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var requestValidation = ValidateRequest(request);
            if (requestValidation is not null)
            {
                return requestValidation;
            }

            var userId = user.GetUserId();
            var transaction = await db.Transactions.SingleOrDefaultAsync(transaction => transaction.UserId == userId && transaction.Id == id, cancellationToken);
            if (transaction is null)
            {
                return Results.NotFound();
            }

            transaction.Update(request.AccountId, request.CategoryId, request.Type, request.Amount, request.Date, request.Description);
            var validation = await ValidateReferences(transaction, db, cancellationToken);
            if (validation is not null)
            {
                return validation;
            }

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(ToResponse(transaction));
        });

        group.MapDelete("/{id:guid}", async (Guid id, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            var transaction = await db.Transactions.SingleOrDefaultAsync(transaction => transaction.UserId == userId && transaction.Id == id, cancellationToken);
            if (transaction is null)
            {
                return Results.NotFound();
            }

            db.Transactions.Remove(transaction);
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });

        return app;
    }

    private static IResult? ValidateRequest(TransactionRequest request)
    {
        if (request.AccountId == Guid.Empty)
        {
            return Results.BadRequest("Account is required.");
        }

        if (request.CategoryId == Guid.Empty)
        {
            return Results.BadRequest("Category is required.");
        }

        if (request.Amount <= 0)
        {
            return Results.BadRequest("Amount must be greater than zero.");
        }

        if (request.Date == default)
        {
            return Results.BadRequest("Date is required.");
        }

        return null;
    }

    private static async Task<IResult?> ValidateReferences(Transaction transaction, FinTrackDbContext db, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.SingleOrDefaultAsync(account => account.Id == transaction.AccountId, cancellationToken);
        if (account is null)
        {
            return Results.BadRequest("Account was not found.");
        }

        var category = await db.Categories.SingleOrDefaultAsync(category => category.Id == transaction.CategoryId, cancellationToken);
        if (category is null)
        {
            return Results.BadRequest("Category was not found.");
        }

        try
        {
            transaction.EnsureAccountBelongsToUser(account);
            transaction.EnsureCategoryIsCompatible(category);
        }
        catch (InvalidOperationException exception)
        {
            return Results.BadRequest(exception.Message);
        }

        return null;
    }

    private static TransactionResponse ToResponse(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.AccountId,
            transaction.CategoryId,
            transaction.Type,
            transaction.Amount,
            transaction.Date,
            transaction.Description,
            transaction.CreatedAt);
}
