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

public sealed class FinancialValidationFlowTests
{
    [Theory]
    [InlineData("0.001")]
    [InlineData("1.005")]
    [InlineData("-0.001")]
    [InlineData("10000000000000000")]
    [InlineData("-10000000000000000")]
    [InlineData("79228162514264337593543950335")]
    public async Task Money_outside_database_precision_is_rejected_without_changes(string value)
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);
        var account = await CreateAccount(client, "Conta", 10);
        var category = await CreateCategory(client, "Mercado", CategoryType.Expense);
        var date = new DateOnly(2026, 8, 16);
        var transaction = await CreateTransaction(client, account.Id, category.Id, TransactionType.Expense, 1, date, null);
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var accountRequest = new AccountRequest("Alterada", account.Type, amount, account.OpeningDate);
        var transactionRequest = new TransactionRequest(account.Id, category.Id, transaction.Type, amount, date, "Alterado");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/accounts", accountRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/accounts/{account.Id}", accountRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/transactions", transactionRequest)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/transactions/{transaction.Id}", transactionRequest)).StatusCode);
        Assert.Equal(account, await client.GetFromJsonAsync<AccountResponse>($"/accounts/{account.Id}"));
        Assert.Equal(transaction, await client.GetFromJsonAsync<TransactionResponse>($"/transactions/{transaction.Id}"));
        Assert.Single((await client.GetFromJsonAsync<List<AccountResponse>>("/accounts"))!);
        Assert.Equal(1, (await client.GetFromJsonAsync<TransactionPageResponse>("/transactions"))!.TotalCount);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("9999999999999999.99")]
    public async Task Money_at_valid_precision_boundaries_is_preserved(string value)
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var account = await CreateAccount(client, "Conta", -amount);
        var category = await CreateCategory(client, "Receita", CategoryType.Income);
        var transaction = await CreateTransaction(client, account.Id, category.Id, TransactionType.Income, amount, new DateOnly(2026, 8, 16), null);
        Assert.Equal(-amount, account.InitialBalance);
        Assert.Equal(amount, transaction.Amount);
        var dashboard = await client.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=2026&month=8");
        Assert.NotNull(dashboard);
        Assert.Equal(0, dashboard.CurrentBalance);
        Assert.Equal(amount, dashboard.TotalIncome);
    }

    [Theory]
    [InlineData("accounts")]
    [InlineData("categories")]
    [InlineData("transactions")]
    public async Task Undefined_financial_types_are_rejected_without_changes(string resource)
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);
        var account = await CreateAccount(client, "Conta");
        var category = await CreateCategory(client, "Mercado", CategoryType.Expense);
        var date = new DateOnly(2026, 8, 16);
        var transaction = await CreateTransaction(client, account.Id, category.Id, TransactionType.Expense, 1, date, null);
        var (id, request) = resource switch
        {
            "accounts" => (account.Id, (object)new AccountRequest("Alterada", (AccountType)999, 10, account.OpeningDate)),
            "categories" => (category.Id, (object)new CategoryRequest("Alterada", (CategoryType)999)),
            _ => (transaction.Id, (object)new TransactionRequest(account.Id, category.Id, (TransactionType)999, 10, date, "Alterado"))
        };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/{resource}", request)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/{resource}/{id}", request)).StatusCode);
        Assert.Equal(account, await client.GetFromJsonAsync<AccountResponse>($"/accounts/{account.Id}"));
        Assert.Equal(category, await client.GetFromJsonAsync<CategoryResponse>($"/categories/{category.Id}"));
        Assert.Equal(transaction, await client.GetFromJsonAsync<TransactionResponse>($"/transactions/{transaction.Id}"));
    }

    [Fact]
    public async Task Edits_preserve_existing_transaction_rules()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);
        var account = await CreateAccount(client, "Conta", 50);
        var category = await CreateCategory(client, "Mercado", CategoryType.Expense);
        var date = new DateOnly(2026, 8, 2);
        await CreateTransaction(client, account.Id, category.Id, TransactionType.Expense, 10, date, null);

        var invalidAccount = await client.PutAsJsonAsync($"/accounts/{account.Id}",
            new AccountRequest("Nome rejeitado", AccountType.Savings, 999, date.AddDays(1)));
        Assert.Equal(HttpStatusCode.Conflict, invalidAccount.StatusCode);
        var unchangedAccount = await client.GetFromJsonAsync<AccountResponse>($"/accounts/{account.Id}");
        Assert.Equal(account, unchangedAccount);

        var validAccount = await client.PutAsJsonAsync($"/accounts/{account.Id}",
            new AccountRequest(account.Name, account.Type, account.InitialBalance, date));
        Assert.Equal(HttpStatusCode.OK, validAccount.StatusCode);

        var invalidCategory = await client.PutAsJsonAsync($"/categories/{category.Id}",
            new CategoryRequest("Nome rejeitado", CategoryType.Income));
        Assert.Equal(HttpStatusCode.Conflict, invalidCategory.StatusCode);
        Assert.Equal(category, await client.GetFromJsonAsync<CategoryResponse>($"/categories/{category.Id}"));

        var rename = await client.PutAsJsonAsync($"/categories/{category.Id}",
            new CategoryRequest("Alimentação", CategoryType.Expense));
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        var dashboard = await client.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=2026&month=8");
        Assert.NotNull(dashboard);
        Assert.Equal(40, dashboard.CurrentBalance);
        Assert.Equal("Alimentação", Assert.Single(dashboard.ExpensesByCategory).CategoryName);
    }

    [Fact]
    public async Task Null_required_fields_return_bad_request()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/auth/register",
            new { name = (string?)null, email = "valid@email.com", password = "Senha@123" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/auth/register",
            new { name = "Nome", email = (string?)null, password = "Senha@123" })).StatusCode);
        await RegisterAndAuthorize(client);
        var account = await CreateAccount(client, "Conta");
        var category = await CreateCategory(client, "Categoria", CategoryType.Expense);
        foreach (var path in new[] { "/accounts", "/categories" })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(path,
                new { name = (string?)null, type = 0 })).StatusCode);
        }
        foreach (var path in new[] { "/auth/me", $"/accounts/{account.Id}", $"/categories/{category.Id}" })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync(path,
                new { name = (string?)null, type = 0 })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/auth/admin/users/{Guid.NewGuid()}/reset-password",
            new { newPassword = (string?)null })).StatusCode);
    }

    [Fact]
    public async Task Accounts_and_categories_with_transactions_cannot_be_deleted()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);

        var account = await CreateAccount(client, "Conta Corrente");
        var category = await CreateCategory(client, "Mercado", CategoryType.Expense);
        await CreateTransaction(client, account.Id, category.Id, TransactionType.Expense, 10, new DateOnly(2026, 8, 21), "Mercado");

        var deleteAccount = await client.DeleteAsync($"/accounts/{account.Id}");
        Assert.Equal(HttpStatusCode.Conflict, deleteAccount.StatusCode);
        Assert.Equal("Exclua os lançamentos desta conta antes de remover a conta.", await deleteAccount.Content.ReadAsStringAsync());

        var deleteCategory = await client.DeleteAsync($"/categories/{category.Id}");
        Assert.Equal(HttpStatusCode.Conflict, deleteCategory.StatusCode);
        Assert.Equal("Exclua os lançamentos desta categoria antes de remover a categoria.", await deleteCategory.Content.ReadAsStringAsync());
    }
}
