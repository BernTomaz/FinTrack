using System.Security.Claims;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            FinTrackDbContext db,
            PasswordHasher passwordHasher,
            JwtTokenService tokenService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest("Name, email and password are required.");
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var exists = await db.Users.AnyAsync(user => user.Email == email, cancellationToken);
            if (exists)
            {
                return Results.Conflict("Email is already registered.");
            }

            var user = new User(request.Name, email, passwordHasher.Hash(request.Password));
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/users/{user.Id}", ToResponse(user, tokenService));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            FinTrackDbContext db,
            PasswordHasher passwordHasher,
            JwtTokenService tokenService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest("Email and password are required.");
            }

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await db.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

            if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(ToResponse(user, tokenService));
        });

        group.MapGet("/me", (ClaimsPrincipal user) =>
            Results.Ok(new
            {
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
                Name = user.FindFirstValue(ClaimTypes.Name),
                Email = user.FindFirstValue(ClaimTypes.Email)
            }))
            .RequireAuthorization();

        return app;
    }

    private static AuthResponse ToResponse(User user, JwtTokenService tokenService) =>
        new(user.Id, user.Name, user.Email, tokenService.Create(user));
}
