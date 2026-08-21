using System.Security.Claims;
using FinTrack.Api.Auth;
using FinTrack.Application.DTOs.Categories;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categories").WithTags("Categories").RequireAuthorization();

        group.MapGet("/", async (FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            var categories = await db.Categories
                .Where(category => category.UserId == userId)
                .OrderBy(category => category.Type)
                .ThenBy(category => category.Name)
                .Select(category => new CategoryResponse(category.Id, category.Name, category.Type, category.CreatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(categories);
        });

        group.MapGet("/{id:guid}", async (Guid id, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            var category = await db.Categories
                .Where(category => category.UserId == userId && category.Id == id)
                .Select(category => new CategoryResponse(category.Id, category.Name, category.Type, category.CreatedAt))
                .SingleOrDefaultAsync(cancellationToken);

            return category is null ? Results.NotFound() : Results.Ok(category);
        });

        group.MapPost("/", async (CategoryRequest request, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest("Name is required.");
            }

            var category = new Category(user.GetUserId(), request.Name, request.Type);
            db.Categories.Add(category);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/categories/{category.Id}", ToResponse(category));
        });

        group.MapPut("/{id:guid}", async (Guid id, CategoryRequest request, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest("Name is required.");
            }

            var userId = user.GetUserId();
            var category = await db.Categories.SingleOrDefaultAsync(category => category.UserId == userId && category.Id == id, cancellationToken);
            if (category is null)
            {
                return Results.NotFound();
            }

            category.Update(request.Name, request.Type);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(ToResponse(category));
        });

        group.MapDelete("/{id:guid}", async (Guid id, FinTrackDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            var category = await db.Categories.SingleOrDefaultAsync(category => category.UserId == userId && category.Id == id, cancellationToken);
            if (category is null)
            {
                return Results.NotFound();
            }

            var hasTransactions = await db.Transactions.AnyAsync(
                transaction => transaction.UserId == userId && transaction.CategoryId == id,
                cancellationToken);
            if (hasTransactions)
            {
                return Results.Text("Exclua os lançamentos desta categoria antes de remover a categoria.", statusCode: StatusCodes.Status409Conflict);
            }

            db.Categories.Remove(category);
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });

        return app;
    }

    private static CategoryResponse ToResponse(Category category) =>
        new(category.Id, category.Name, category.Type, category.CreatedAt);
}
