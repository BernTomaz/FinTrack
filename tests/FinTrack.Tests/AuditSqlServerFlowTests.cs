using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinTrack.Api.Auth;
using FinTrack.Application.DTOs.Accounts;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Application.DTOs.Transactions;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static FinTrack.Tests.ApiTestData;

namespace FinTrack.Tests;

public sealed class AuditSqlServerFlowTests
{
    [SqlServerFact]
    public async Task Audit_queries_rollback_and_retention_work_in_a_disposable_sql_database()
    {
        var database = $"FinTrackAuditTests_{Guid.NewGuid():N}";
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("FINTRACK_TEST_SQLSERVER"))
        {
            InitialCatalog = database
        }.ConnectionString;
        await using var app = new FinTrackApiFactory(connection);
        try
        {
            // Exercises the actual endpoint projections, filters and authorization on SQL Server.
            await AuditQueryFlowTests.VerifyAuditQueries(app);
            using var client = app.CreateClient();
            var owner = await RegisterAndAuthorize(client, "Atomic", "atomic@fintrack.local");
            var account = await CreateAccount(client, "Original", 100);
            var category = await CreateCategory(client, "Mercado", CategoryType.Expense);
            var transaction = await CreateTransaction(client, account.Id, category.Id,
                TransactionType.Expense, 10, new DateOnly(2026, 8, 16), null);
            for (var attempt = 0; attempt < 3; attempt++)
                await client.PostAsJsonAsync("/auth/login", new LoginRequest(owner.Email, "errada"));

            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
            var eventCount = await db.AuditEvents.CountAsync();
            var originalHash = await db.Users.Where(u => u.Id == owner.UserId).Select(u => u.PasswordHash).SingleAsync();
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE [AuditEvents] ADD CONSTRAINT [CK_RejectTestAudit]
                CHECK ([CorrelationId] <> 'force-audit-failure');
                """);
            client.DefaultRequestHeaders.Add("X-Correlation-Id", "force-audit-failure");
            Assert.Equal(HttpStatusCode.InternalServerError, (await client.PutAsJsonAsync($"/accounts/{account.Id}",
                new AccountRequest("Changed", AccountType.Checking, 999, account.OpeningDate))).StatusCode);
            Assert.Equal(HttpStatusCode.InternalServerError, (await client.PutAsJsonAsync($"/transactions/{transaction.Id}",
                new TransactionRequest(account.Id, category.Id, TransactionType.Expense, 99, transaction.Date, "Changed"))).StatusCode);
            Assert.Equal(HttpStatusCode.InternalServerError, (await client.DeleteAsync($"/transactions/{transaction.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.InternalServerError, (await client.PutAsJsonAsync("/auth/password",
                new ChangePasswordRequest("Senha@123", "Changed@123"))).StatusCode);

            using var admin = app.CreateClient();
            var login = await admin.PostAsJsonAsync("/auth/login", new LoginRequest("admin-audit@fintrack.local", "Senha@123"));
            login.EnsureSuccessStatusCode();
            var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
            admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
            admin.DefaultRequestHeaders.Add("X-Correlation-Id", "force-audit-failure");
            Assert.Equal(HttpStatusCode.InternalServerError, (await admin.PostAsJsonAsync($"/auth/admin/users/{owner.UserId}/reset-password",
                new AdminResetPasswordRequest("Changed@123"))).StatusCode);

            Assert.Equal(100, (await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == account.Id)).InitialBalance);
            Assert.Equal("Original", (await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == account.Id)).Name);
            Assert.Equal(10, (await db.Transactions.AsNoTracking().SingleAsync(t => t.Id == transaction.Id)).Amount);
            var unchangedUser = await db.Users.AsNoTracking().SingleAsync(u => u.Id == owner.UserId);
            Assert.Equal(originalHash, unchangedUser.PasswordHash);
            Assert.True(unchangedUser.IsLocked);
            Assert.Equal(3, unchangedUser.FailedLoginAttempts);
            Assert.Equal(eventCount, await db.AuditEvents.CountAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE [AuditEvents] DROP CONSTRAINT [CK_RejectTestAudit];");
            client.DefaultRequestHeaders.Remove("X-Correlation-Id");
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/accounts/{account.Id}",
                new AccountRequest("Saved", AccountType.Checking, 250, account.OpeningDate))).StatusCode);

            var cutoff = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var baseline = await db.AuditEvents.CountAsync();
            var seed = new AuditEvent(owner.UserId, owner.UserId, "Created", "Account", account.Id, null, null, "retention-seed");
            db.AuditEvents.Add(seed);
            db.Entry(seed).Property(e => e.OccurredAtUtc).CurrentValue = cutoff.AddDays(-1);
            var boundary = new AuditEvent(owner.UserId, owner.UserId, "Created", "Account", account.Id, null, null, "boundary");
            db.AuditEvents.Add(boundary);
            db.Entry(boundary).Property(e => e.OccurredAtUtc).CurrentValue = cutoff;
            var newer = new AuditEvent(owner.UserId, owner.UserId, "Created", "Account", account.Id, null, null, "newer");
            db.AuditEvents.Add(newer);
            db.Entry(newer).Property(e => e.OccurredAtUtc).CurrentValue = cutoff.AddTicks(1);
            await db.SaveChangesAsync();
            // Seed 10,000 additional expired rows to verify the real per-run deletion limit.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [AuditEvents] ([Id], [ActorUserId], [OwnerUserId], [Action], [EntityType],
                    [EntityId], [OccurredAtUtc], [BeforeJson], [AfterJson], [CorrelationId])
                SELECT TOP (10000) NEWID(), [ActorUserId], [OwnerUserId], [Action], [EntityType],
                    [EntityId], [OccurredAtUtc], NULL, NULL, 'retention-batch'
                FROM [AuditEvents] CROSS JOIN sys.all_objects AS a CROSS JOIN sys.all_objects AS b
                WHERE [AuditEvents].[Id] = {seed.Id};
                """);
            var sql = (await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "prune-audit.sql")))
                .Replace("$(DatabaseName)", database);
            async Task Prune(bool apply) => await db.Database.ExecuteSqlRawAsync(sql,
                new SqlParameter("@Cutoff", cutoff), new SqlParameter("@Apply", apply));
            await Prune(false);
            Assert.Equal(baseline + 10003, await db.AuditEvents.CountAsync());
            await Prune(true);
            Assert.Equal(baseline + 3, await db.AuditEvents.CountAsync());
            await Prune(true);
            Assert.Equal(baseline + 2, await db.AuditEvents.CountAsync());
            Assert.True(await db.AuditEvents.AnyAsync(e => e.Id == boundary.Id));
            Assert.True(await db.AuditEvents.AnyAsync(e => e.Id == newer.Id));
            Assert.Equal(250, (await db.Accounts.AsNoTracking().SingleAsync(a => a.Id == account.Id)).InitialBalance);
            Assert.True(await db.Transactions.AnyAsync(t => t.Id == transaction.Id));
        }
        finally
        {
            await using var cleanup = new FinTrackDbContext(new DbContextOptionsBuilder<FinTrackDbContext>().UseSqlServer(connection).Options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
