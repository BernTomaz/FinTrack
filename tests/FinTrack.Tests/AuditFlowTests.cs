using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinTrack.Application.DTOs.Transactions;
using FinTrack.Application.DTOs.Accounts;
using FinTrack.Application.DTOs.Categories;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static FinTrack.Tests.ApiTestData;

namespace FinTrack.Tests;

public sealed class AuditFlowTests
{
    [Theory]
    [InlineData("accounts", "Account")]
    [InlineData("categories", "Category")]
    public async Task Account_and_category_history_preserves_changes_and_rejects_other_users(string route, string entityType)
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        var owner = await RegisterAndAuthorize(client);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "account-category-audit");
        var opening = new DateOnly(2026, 1, 1);
        var id = route == "accounts"
            ? (await CreateAccount(client, "Original", 100, opening)).Id
            : (await CreateCategory(client, "Original", CategoryType.Expense)).Id;
        object edit = route == "accounts"
            ? new AccountRequest("Editado", AccountType.Checking, 250, opening.AddDays(1))
            : new CategoryRequest("Editado", CategoryType.Income);
        var url = $"/{route}/{id}";
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(url, edit)).StatusCode);
        using var other = app.CreateClient();
        await RegisterAndAuthorize(other, "Outro", "other-audit@fintrack.local");
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync(url, edit)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(url)).StatusCode);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var events = await db.AuditEvents.Where(e => e.EntityId == id).ToListAsync();
        Assert.Equal(3, events.Count);
        var created = Assert.Single(events, e => e.Action == "Created");
        var updated = Assert.Single(events, e => e.Action == "Updated");
        var deleted = Assert.Single(events, e => e.Action == "Deleted");
        Assert.Null(created.BeforeJson);
        Assert.Equal(created.AfterJson, updated.BeforeJson);
        Assert.Equal(updated.AfterJson, deleted.BeforeJson);
        Assert.Null(deleted.AfterJson);
        using var before = JsonDocument.Parse(updated.BeforeJson!);
        using var after = JsonDocument.Parse(updated.AfterJson!);
        Assert.Equal("Original", before.RootElement.GetProperty("Name").GetString());
        Assert.Equal("Editado", after.RootElement.GetProperty("Name").GetString());
        if (route == "accounts")
        {
            Assert.Equal(100, before.RootElement.GetProperty("InitialBalance").GetDecimal());
            Assert.Equal(250, after.RootElement.GetProperty("InitialBalance").GetDecimal());
            Assert.Equal("2026-01-01", before.RootElement.GetProperty("OpeningDate").GetString());
            Assert.Equal("2026-01-02", after.RootElement.GetProperty("OpeningDate").GetString());
        }
        else
        {
            Assert.Equal("Expense", before.RootElement.GetProperty("Type").GetString());
            Assert.Equal("Income", after.RootElement.GetProperty("Type").GetString());
        }
        Assert.All(events, e =>
        {
            Assert.Equal(entityType, e.EntityType);
            Assert.Equal(owner.UserId, e.ActorUserId);
            Assert.Equal(owner.UserId, e.OwnerUserId);
            Assert.Equal("account-category-audit", e.CorrelationId);
        });
    }

    [Fact]
    public async Task Linked_accounts_and_categories_do_not_record_rejected_updates_or_deletes()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);
        var account = await CreateAccount(client, "Conta");
        var category = await CreateCategory(client, "Mercado", CategoryType.Expense);
        await CreateTransaction(client, account.Id, category.Id, TransactionType.Expense, 10, new DateOnly(2026, 8, 16), null);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/accounts/{account.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"/categories/{category.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/accounts/{account.Id}",
            new AccountRequest("Alterada", AccountType.Checking, 900, new DateOnly(2026, 9, 1)))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/categories/{category.Id}",
            new CategoryRequest("Alterada", CategoryType.Income))).StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
        var events = await db.AuditEvents.Where(e => e.EntityId == account.Id || e.EntityId == category.Id).ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal("Created", e.Action));
        Assert.Equal("Conta", (await db.Accounts.SingleAsync(a => a.Id == account.Id)).Name);
        Assert.Equal("Mercado", (await db.Categories.SingleAsync(c => c.Id == category.Id)).Name);
    }

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
