using System.Net.Mail;
using System.Security.Claims;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Auth;

public static class AuthEndpoints
{
    private const int MaxLoginAttempts = 3;
    private const string LockedMessage = "Conta bloqueada por excesso de tentativas. Entre em contato com o administrador para redefinir sua senha.";

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
            var name = request.Name.Trim();
            var email = request.Email.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest("Name, email and password are required.");
            }

            if (name.Length < 2 || name.Length > 80)
            {
                return Results.BadRequest("Name must have between 2 and 80 characters.");
            }

            if (!IsValidEmail(email) || email.Length > 120)
            {
                return Results.BadRequest("Email is invalid.");
            }

            if (!IsStrongPassword(request.Password))
            {
                return Results.BadRequest("A senha deve ter de 8 a 100 caracteres, com letra maiúscula, letra minúscula, número e caractere especial.");
            }

            var emailExists = await db.Users.AnyAsync(user => user.Email == email, cancellationToken);
            if (emailExists)
            {
                return Results.Conflict("Este e-mail já está em uso.");
            }

            var normalizedName = name.ToLowerInvariant();
            var nameExists = await db.Users.AnyAsync(user => user.Name.ToLower() == normalizedName, cancellationToken);
            if (nameExists)
            {
                return Results.Conflict("Este nome já está em uso.");
            }

            var isFirstUser = !await db.Users.AnyAsync(cancellationToken);
            var user = new User(name, email, passwordHasher.Hash(request.Password), isFirstUser);
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
            if (!IsValidEmail(email) || email.Length > 120)
            {
                return Results.BadRequest("Email is invalid.");
            }

            var user = await db.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

            if (user is null)
            {
                return Results.Unauthorized();
            }

            if (user.IsLocked)
            {
                return Results.Text(LockedMessage, statusCode: StatusCodes.Status423Locked);
            }

            if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                user.RegisterFailedLogin(MaxLoginAttempts);
                await db.SaveChangesAsync(cancellationToken);

                if (user.IsLocked)
                {
                    return Results.Text(LockedMessage, statusCode: StatusCodes.Status423Locked);
                }

                return Results.Unauthorized();
            }

            user.ResetLoginAttempts();
            await db.SaveChangesAsync(cancellationToken);

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

        group.MapPut("/me", async (
            UpdateProfileRequest request,
            FinTrackDbContext db,
            ClaimsPrincipal principal,
            JwtTokenService tokenService,
            CancellationToken cancellationToken) =>
        {
            var name = request.Name.Trim();
            if (name.Length < 2 || name.Length > 80)
            {
                return Results.BadRequest("Name must have between 2 and 80 characters.");
            }

            var userId = principal.GetUserId();
            var normalizedName = name.ToLowerInvariant();
            var exists = await db.Users.AnyAsync(user => user.Id != userId && user.Name.ToLower() == normalizedName, cancellationToken);
            if (exists)
            {
                return Results.Conflict("Este nome já está em uso.");
            }

            var user = await db.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.UpdateName(name);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(ToResponse(user, tokenService));
        })
        .RequireAuthorization();

        group.MapPut("/password", async (
            ChangePasswordRequest request,
            FinTrackDbContext db,
            PasswordHasher passwordHasher,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Results.BadRequest("Current password and new password are required.");
            }

            if (!IsStrongPassword(request.NewPassword))
            {
                return Results.BadRequest("A nova senha deve ter de 8 a 100 caracteres, com letra maiúscula, letra minúscula, número e caractere especial.");
            }

            var user = await db.Users.SingleOrDefaultAsync(user => user.Id == principal.GetUserId(), cancellationToken);
            if (user is null)
            {
                return Results.NotFound();
            }

            if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            user.ChangePassword(passwordHasher.Hash(request.NewPassword));
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        })
        .RequireAuthorization();

        group.MapGet("/admin/locked-users", async (
            FinTrackDbContext db,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!await IsAdmin(db, principal, cancellationToken))
            {
                return Results.Forbid();
            }

            var users = await db.Users
                .Where(user => user.IsLocked)
                .OrderBy(user => user.Name)
                .Select(user => new LockedUserResponse(user.Id, user.Name, user.Email, user.FailedLoginAttempts))
                .ToListAsync(cancellationToken);

            return Results.Ok(users);
        })
        .RequireAuthorization();

        group.MapPost("/admin/users/{id:guid}/reset-password", async (
            Guid id,
            AdminResetPasswordRequest request,
            FinTrackDbContext db,
            PasswordHasher passwordHasher,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken) =>
        {
            if (!await IsAdmin(db, principal, cancellationToken))
            {
                return Results.Forbid();
            }

            if (!IsStrongPassword(request.NewPassword))
            {
                return Results.BadRequest("A senha temporária deve ter de 8 a 100 caracteres, com letra maiúscula, letra minúscula, número e caractere especial.");
            }

            var user = await db.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.ResetBlockedPassword(passwordHasher.Hash(request.NewPassword));
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        })
        .RequireAuthorization();

        return app;
    }

    private static AuthResponse ToResponse(User user, JwtTokenService tokenService) =>
        new(user.Id, user.Name, user.Email, user.IsAdmin, tokenService.Create(user));

    private static async Task<bool> IsAdmin(FinTrackDbContext db, ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = principal.GetUserId();
        return await db.Users.AnyAsync(user => user.Id == userId && user.IsAdmin, cancellationToken);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            return new MailAddress(email).Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsStrongPassword(string password) =>
        password.Length is >= 8 and <= 100 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit) &&
        password.Any(character => !char.IsLetterOrDigit(character));
}
