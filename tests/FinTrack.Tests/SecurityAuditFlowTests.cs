using System.Net;
using System.Net.Http.Json;
using FinTrack.Api.Auth;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static FinTrack.Tests.ApiTestData;

namespace FinTrack.Tests;

public sealed class SecurityAuditFlowTests
{
    [Fact]
    public async Task Password_change_records_only_success_without_credential_snapshots()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        var auth = await RegisterAndAuthorize(client);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "password-audit");
        using var anonymous = app.CreateClient();
        var change = new ChangePasswordRequest("Senha@123", "Nova@123");
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync("/auth/password", change)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/auth/password", change with { CurrentPassword = "errada" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/auth/password", change with { NewPassword = "fraca" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync("/auth/password", change)).StatusCode);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var audit = Assert.Single(await db.AuditEvents.ToListAsync());
        Assert.Equal("PasswordChanged", audit.Action);
        Assert.Equal("User", audit.EntityType);
        Assert.Equal(auth.UserId, audit.ActorUserId);
        Assert.Equal(auth.UserId, audit.OwnerUserId);
        Assert.Equal(auth.UserId, audit.EntityId);
        Assert.Equal("password-audit", audit.CorrelationId);
        Assert.Equal(DateTimeKind.Utc, audit.OccurredAtUtc.Kind);
        Assert.Null(audit.BeforeJson);
        Assert.Null(audit.AfterJson);
        var user = await db.Users.SingleAsync(u => u.Id == auth.UserId);
        Assert.True(scope.ServiceProvider.GetRequiredService<PasswordHasher>().Verify(change.NewPassword, user.PasswordHash));
    }

    [Fact]
    public async Task Admin_reset_records_admin_and_target_and_ignores_rejected_requests()
    {
        await using var app = new FinTrackApiFactory();
        using var admin = app.CreateClient();
        var actor = await RegisterAndAuthorize(admin, "Admin", "admin-audit@fintrack.local");
        using var client = app.CreateClient();
        var target = await RegisterAndAuthorize(client);
        for (var i = 0; i < 3; i++)
            await client.PostAsJsonAsync("/auth/login", new LoginRequest(target.Email, "errada"));
        var url = $"/auth/admin/users/{target.UserId}/reset-password";
        var reset = new AdminResetPasswordRequest("Reset@123");
        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(url, reset)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync(url, reset)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync(url, new AdminResetPasswordRequest("fraca"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsJsonAsync($"/auth/admin/users/{Guid.NewGuid()}/reset-password", reset)).StatusCode);
        admin.DefaultRequestHeaders.Add("X-Correlation-Id", "admin-password-audit");
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsJsonAsync(url, reset)).StatusCode);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var audit = Assert.Single(await db.AuditEvents.ToListAsync());
        Assert.Equal("AdminPasswordReset", audit.Action);
        Assert.Equal("User", audit.EntityType);
        Assert.Equal(actor.UserId, audit.ActorUserId);
        Assert.Equal(target.UserId, audit.OwnerUserId);
        Assert.Equal(target.UserId, audit.EntityId);
        Assert.Equal("admin-password-audit", audit.CorrelationId);
        Assert.Null(audit.BeforeJson);
        Assert.Null(audit.AfterJson);
        var user = await db.Users.SingleAsync(u => u.Id == target.UserId);
        Assert.False(user.IsLocked);
        Assert.Equal(0, user.FailedLoginAttempts);
        Assert.True(scope.ServiceProvider.GetRequiredService<PasswordHasher>().Verify(reset.NewPassword, user.PasswordHash));
    }
}
