using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinTrack.Application.DTOs.Accounts;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Application.DTOs.Categories;
using FinTrack.Application.DTOs.Dashboard;
using FinTrack.Application.DTOs.Transactions;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

using static FinTrack.Tests.ApiTestData;

namespace FinTrack.Tests;

public sealed class TransactionFlowTests
{
    [Fact]
    public async Task Transaction_edits_and_deletion_recalculate_exact_balances_across_months()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await VerifyTransactionBalances(client);
    }

    [Fact]
    public async Task Transactions_flow_works_with_filters()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);

        var account = await CreateAccount(client, "Conta Corrente");
        var expenseCategory = await CreateCategory(client, "Mercado", CategoryType.Expense);
        var incomeCategory = await CreateCategory(client, "Salário", CategoryType.Income);
        var date = new DateOnly(2026, 8, 16);

        var create = await client.PostAsJsonAsync("/transactions", new TransactionRequest(
            account.Id,
            expenseCategory.Id,
            TransactionType.Expense,
            25,
            date,
            " Mercado "));
        Assert.True(create.StatusCode == HttpStatusCode.Created, await create.Content.ReadAsStringAsync());

        var created = await create.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(created);
        Assert.Equal("Mercado", created.Description);

        var all = await client.GetFromJsonAsync<TransactionPageResponse>("/transactions");
        Assert.NotNull(all);
        Assert.Single(all.Items);

        var filtered = await client.GetFromJsonAsync<TransactionPageResponse>(
            $"/transactions?year=2026&month=8&type=Expense&accountId={account.Id}&categoryId={expenseCategory.Id}");
        Assert.NotNull(filtered);
        Assert.Single(filtered.Items);

        var byYear = await client.GetFromJsonAsync<TransactionPageResponse>("/transactions?year=2026");
        Assert.NotNull(byYear);
        Assert.Single(byYear.Items);

        var byMonth = await client.GetFromJsonAsync<TransactionPageResponse>("/transactions?month=8");
        Assert.NotNull(byMonth);
        Assert.Single(byMonth.Items);

        var byId = await client.GetFromJsonAsync<TransactionResponse>($"/transactions/{created.Id}");
        Assert.NotNull(byId);
        Assert.Equal(created.Id, byId.Id);

        var update = await client.PutAsJsonAsync($"/transactions/{created.Id}", new TransactionRequest(
            account.Id,
            incomeCategory.Id,
            TransactionType.Income,
            100,
            date,
            "Salário"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var delete = await client.DeleteAsync($"/transactions/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Transactions_return_expected_errors()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);

        var account = await CreateAccount(client, "Conta Corrente");
        var category = await CreateCategory(client, "Mercado", CategoryType.Expense);
        var date = new DateOnly(2026, 8, 16);

        var missing = await client.GetAsync($"/transactions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var invalid = await client.PostAsJsonAsync("/transactions", new TransactionRequest(Guid.Empty, category.Id, TransactionType.Expense, 10, date, null));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var invalidCategoryId = await client.PostAsJsonAsync("/transactions", new TransactionRequest(account.Id, Guid.Empty, TransactionType.Expense, 10, date, null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidCategoryId.StatusCode);

        var invalidDate = await client.PostAsJsonAsync("/transactions", new TransactionRequest(account.Id, category.Id, TransactionType.Expense, 10, default, null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidDate.StatusCode);

        var beforeAccountOpening = await client.PostAsJsonAsync("/transactions", new TransactionRequest(account.Id, category.Id, TransactionType.Expense, 10, new DateOnly(2025, 12, 31), null));
        Assert.Equal(HttpStatusCode.BadRequest, beforeAccountOpening.StatusCode);

        var invalidDescription = await client.PostAsJsonAsync("/transactions", new TransactionRequest(account.Id, category.Id, TransactionType.Expense, 10, date, new string('a', 161)));
        Assert.Equal(HttpStatusCode.BadRequest, invalidDescription.StatusCode);

        var maxDescription = await client.PostAsJsonAsync("/transactions", new TransactionRequest(account.Id, category.Id, TransactionType.Expense, 10, date, new string('a', 160)));
        Assert.Equal(HttpStatusCode.Created, maxDescription.StatusCode);

        var invalidAccount = await client.PostAsJsonAsync("/transactions", new TransactionRequest(Guid.NewGuid(), category.Id, TransactionType.Expense, 10, date, null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidAccount.StatusCode);

        var invalidCategory = await client.PostAsJsonAsync("/transactions", new TransactionRequest(account.Id, Guid.NewGuid(), TransactionType.Expense, 10, date, null));
        Assert.Equal(HttpStatusCode.BadRequest, invalidCategory.StatusCode);

        var wrongType = await client.PostAsJsonAsync("/transactions", new TransactionRequest(account.Id, category.Id, TransactionType.Income, 10, date, null));
        Assert.Equal(HttpStatusCode.BadRequest, wrongType.StatusCode);

        var createdResponse = await client.PostAsJsonAsync("/transactions", new TransactionRequest(account.Id, category.Id, TransactionType.Expense, 10, date, null));
        var created = await createdResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(created);

        var updateMissing = await client.PutAsJsonAsync($"/transactions/{Guid.NewGuid()}", new TransactionRequest(account.Id, category.Id, TransactionType.Expense, 10, date, null));
        Assert.Equal(HttpStatusCode.NotFound, updateMissing.StatusCode);

        var updateInvalid = await client.PutAsJsonAsync($"/transactions/{created.Id}", new TransactionRequest(account.Id, category.Id, TransactionType.Expense, 0, date, null));
        Assert.Equal(HttpStatusCode.BadRequest, updateInvalid.StatusCode);

        var updateWrongType = await client.PutAsJsonAsync($"/transactions/{created.Id}", new TransactionRequest(account.Id, category.Id, TransactionType.Income, 10, date, null));
        Assert.Equal(HttpStatusCode.BadRequest, updateWrongType.StatusCode);

        var deleteMissing = await client.DeleteAsync($"/transactions/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, deleteMissing.StatusCode);
    }

    [Fact]
    public async Task Transactions_are_paged_filtered_sorted_and_isolated()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await VerifyTransactionPages(client);
    }
}
