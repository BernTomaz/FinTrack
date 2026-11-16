using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinTrack.Application.DTOs.Transactions;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static FinTrack.Tests.ApiTestData;

namespace FinTrack.Tests;

public sealed class AuditFlowTests
{
    [Fact]
    public async Task Transaction_history_preserves_snapshots_and_ignores_rejected_changes()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "audit-test");
        var account = await CreateAccount(client, "Conta");
        var category = await CreateCategory(client, "Mercado", CategoryType.Expense);
        var date = new DateOnly(2026, 8, 16);
        var transaction = await CreateTransaction(client, account.Id, category.Id,
            TransactionType.Expense, 25, date, "Original");
        var edit = new TransactionRequest(account.Id, category.Id, TransactionType.Expense, 50, date, "Editado");
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/transactions/{transaction.Id}", edit)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/transactions/{transaction.Id}", edit with { AccountId = Guid.NewGuid() })).StatusCode);
        using var other = app.CreateClient();
        await RegisterAndAuthorize(other, "Outro", "other-audit@fintrack.local");
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/transactions/{transaction.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/transactions/{transaction.Id}")).StatusCode);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var events = await db.AuditEvents.Where(e => e.EntityId == transaction.Id).ToListAsync();
        Assert.Equal(3, events.Count);
        var created = Assert.Single(events, e => e.Action == "Created");
        var updated = Assert.Single(events, e => e.Action == "Updated");
        var deleted = Assert.Single(events, e => e.Action == "Deleted");
        Assert.Null(created.BeforeJson);
        Assert.Equal(created.AfterJson, updated.BeforeJson);
        Assert.Equal(updated.AfterJson, deleted.BeforeJson);
        Assert.Null(deleted.AfterJson);
        Assert.Equal(25, JsonDocument.Parse(updated.BeforeJson!).RootElement.GetProperty("Amount").GetDecimal());
        Assert.Equal(50, JsonDocument.Parse(updated.AfterJson!).RootElement.GetProperty("Amount").GetDecimal());
        Assert.All(events, e =>
        {
            Assert.Equal("audit-test", e.CorrelationId);
            Assert.Equal(account.Id, JsonDocument.Parse(e.BeforeJson ?? e.AfterJson!).RootElement.GetProperty("AccountId").GetGuid());
            Assert.NotEqual(Guid.Empty, e.ActorUserId);
            Assert.Equal(e.ActorUserId, e.OwnerUserId);
        });
        Assert.False(await db.Transactions.AnyAsync(t => t.Id == transaction.Id));
    }
}
