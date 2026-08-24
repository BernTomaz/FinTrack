using System.Security.Claims;
using FinTrack.Api.Auth;
using FinTrack.Application.DTOs.Accounts;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts").WithTags("Accounts").RequireAuthorization();

        group.MapGet("/", async (FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            var accounts = await db.Accounts
                .Where(account => account.UserId == userId)
                .OrderBy(account => account.Name)
                .Select(account => new AccountResponse(account.Id, account.Name, account.Type, account.InitialBalance, account.CreatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(accounts);
        });

        group.MapGet("/{id:guid}", async (Guid id, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            var account = await db.Accounts
                .Where(account => account.UserId == userId && account.Id == id)
                .Select(account => new AccountResponse(account.Id, account.Name, account.Type, account.InitialBalance, account.CreatedAt))
                .SingleOrDefaultAsync(cancellationToken);

            return account is null ? Results.NotFound() : Results.Ok(account);
        });

        group.MapPost("/", async (AccountRequest request, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var validation = ValidateRequest(request);
            if (validation is not null)
            {
                return validation;
            }

            var account = new Account(user.GetUserId(), request.Name, request.Type, request.InitialBalance);
            db.Accounts.Add(account);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/accounts/{account.Id}", ToResponse(account));
        });

        group.MapPut("/{id:guid}", async (Guid id, AccountRequest request, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var validation = ValidateRequest(request);
            if (validation is not null)
            {
                return validation;
            }

            var userId = user.GetUserId();
            var account = await db.Accounts.SingleOrDefaultAsync(account => account.UserId == userId && account.Id == id, cancellationToken);
            if (account is null)
            {
                return Results.NotFound();
            }

            account.Update(request.Name, request.Type, request.InitialBalance);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(ToResponse(account));
        });

        group.MapDelete("/{id:guid}", async (Guid id, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            var account = await db.Accounts.SingleOrDefaultAsync(account => account.UserId == userId && account.Id == id, cancellationToken);
            if (account is null)
            {
                return Results.NotFound();
            }

            var hasTransactions = await db.Transactions.AnyAsync(
                transaction => transaction.UserId == userId && transaction.AccountId == id,
                cancellationToken);
            if (hasTransactions)
            {
                return Results.Text("Exclua os lançamentos desta conta antes de remover a conta.", statusCode: StatusCodes.Status409Conflict);
            }

            db.Accounts.Remove(account);
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });

        return app;
    }

    private static AccountResponse ToResponse(Account account) =>
        new(account.Id, account.Name, account.Type, account.InitialBalance, account.CreatedAt);

    private static IResult? ValidateRequest(AccountRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            return Results.BadRequest("Name is required.");
        }

        if (name.Length < 2 || name.Length > 80)
        {
            return Results.BadRequest("Name must have between 2 and 80 characters.");
        }

        return null;
    }
}
