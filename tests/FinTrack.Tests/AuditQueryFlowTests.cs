using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinTrack.Application.DTOs.Audit;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static FinTrack.Tests.ApiTestData;

namespace FinTrack.Tests;

public sealed class AuditQueryFlowTests
{
    [Fact]
    public async Task Audit_queries_are_paginated_filtered_and_private_even_for_admin_actors()
    {
        await using var app = new FinTrackApiFactory();
        using var admin = app.CreateClient();
        var actor = await RegisterAndAuthorize(admin, "Admin", "admin-audit@fintrack.local");
        using var client = app.CreateClient();
        var owner = await RegisterAndAuthorize(client);
        var entityId = Guid.NewGuid();
        var first = new AuditEvent(owner.UserId, owner.UserId, "Created", "Account", entityId, null, "{\"Name\":\"Original\"}", "one");
        var second = new AuditEvent(owner.UserId, owner.UserId, "Updated", "Account", entityId, first.AfterJson, "{\"Name\":\"Editada\"}", "two");
        var third = new AuditEvent(actor.UserId, owner.UserId, "AdminPasswordReset", "User", owner.UserId, null, null, "three");
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
            db.AuditEvents.AddRange(first, second, third);
            db.Entry(first).Property(e => e.OccurredAtUtc).CurrentValue = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            db.Entry(second).Property(e => e.OccurredAtUtc).CurrentValue = new DateTime(2026, 1, 2, 23, 59, 59, DateTimeKind.Utc);
            db.Entry(third).Property(e => e.OccurredAtUtc).CurrentValue = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);
            await db.SaveChangesAsync();
        }
        using var anonymous = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/audit-events")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/audit-events/{first.Id}")).StatusCode);
        var page = await client.GetFromJsonAsync<AuditEventPageResponse>("/audit-events?page=1&pageSize=2");
        Assert.NotNull(page);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(new[] { third.Id, second.Id }, page.Items.Select(e => e.Id));
        Assert.Equal(DateTimeKind.Utc, page.Items[0].OccurredAtUtc.Kind);
        var next = await client.GetFromJsonAsync<AuditEventPageResponse>("/audit-events?page=2&pageSize=2");
        Assert.Equal(first.Id, Assert.Single(next!.Items).Id);
        var filtered = await client.GetFromJsonAsync<AuditEventPageResponse>($"/audit-events?entityType=Account&entityId={entityId}&action=Updated&from=2026-01-02&to=2026-01-02");
        Assert.Equal(second.Id, Assert.Single(filtered!.Items).Id);
        using var listJson = JsonDocument.Parse(await client.GetStringAsync("/audit-events"));
        Assert.False(listJson.RootElement.GetProperty("items")[0].TryGetProperty("beforeJson", out _));
        var detail = await client.GetFromJsonAsync<AuditEventDetailResponse>($"/audit-events/{second.Id}");
        Assert.Equal(first.AfterJson, detail!.BeforeJson);
        Assert.Equal("two", detail.CorrelationId);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/audit-events/{third.Id}")).StatusCode);
        Assert.Empty((await admin.GetFromJsonAsync<AuditEventPageResponse>("/audit-events"))!.Items);
        Assert.Empty((await admin.GetFromJsonAsync<AuditEventPageResponse>($"/audit-events?entityId={entityId}"))!.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/audit-events/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.DeleteAsync($"/audit-events/{first.Id}")).StatusCode);
        using var finalScope = app.Services.CreateScope();
        Assert.Equal(3, await finalScope.ServiceProvider.GetRequiredService<FinTrackDbContext>().AuditEvents.CountAsync());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=abc")]
    [InlineData("pageSize=abc")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=0")]
    [InlineData("page=2147483647&pageSize=100")]
    [InlineData("entityType=Unknown")]
    [InlineData("action=Unknown")]
    [InlineData("entityId=invalid")]
    [InlineData("from=invalid")]
    [InlineData("from=2026-02-01&to=2026-01-01")]
    public async Task Audit_query_rejects_invalid_filters(string query)
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/audit-events?{query}")).StatusCode);
    }
}
