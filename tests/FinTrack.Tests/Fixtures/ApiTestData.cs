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

namespace FinTrack.Tests;

internal static class ApiTestData
{
    internal static async Task VerifyTransactionBalances(HttpClient client)
    {
        await RegisterAndAuthorize(client, "Saldos", "balances@fintrack.local");
        var account = await CreateAccount(client, "Conta", 100.10m);
        var income = await CreateCategory(client, "Salário", CategoryType.Income);
        var expense = await CreateCategory(client, "Mercado", CategoryType.Expense);
        var date = new DateOnly(2026, 1, 31);
        var credit = await CreateTransaction(client, account.Id, income.Id, TransactionType.Income, 10.20m, date, null);
        var debit = await CreateTransaction(client, account.Id, expense.Id, TransactionType.Expense, 0.30m, date, null);

        var january = await client.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=2026&month=1");
        Assert.NotNull(january);
        Assert.Equal(10.20m, january.TotalIncome);
        Assert.Equal(0.30m, january.TotalExpense);
        Assert.Equal(9.90m, january.MonthBalance);
        Assert.Equal(110.00m, january.CurrentBalance);
        Assert.Equal(0.30m, Assert.Single(january.ExpensesByCategory).Total);

        var update = await client.PutAsJsonAsync($"/transactions/{debit.Id}", new TransactionRequest(
            account.Id, income.Id, TransactionType.Income, 5.15m, date.AddDays(1), "Crédito corrigido"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        january = await client.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=2026&month=1");
        Assert.NotNull(january);
        Assert.Equal(10.20m, january.MonthBalance);
        Assert.Equal(110.30m, january.CurrentBalance);
        Assert.Equal(0, january.TotalExpense);
        Assert.Empty(january.ExpensesByCategory);
        Assert.Equal(credit.Id, Assert.Single(january.LatestTransactions).Id);
        var february = await client.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=2026&month=2");
        Assert.NotNull(february);
        Assert.Equal(5.15m, february.TotalIncome);
        Assert.Equal(0, february.TotalExpense);
        Assert.Equal(5.15m, february.MonthBalance);
        Assert.Equal(115.45m, february.CurrentBalance);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/transactions/{debit.Id}")).StatusCode);
        february = await client.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=2026&month=2");
        Assert.NotNull(february);
        Assert.Equal(0, february.TotalIncome);
        Assert.Equal(0, february.MonthBalance);
        Assert.Equal(110.30m, february.CurrentBalance);
        Assert.Empty(february.LatestTransactions);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/transactions/{debit.Id}")).StatusCode);
        Assert.DoesNotContain("Crédito corrigido", await client.GetStringAsync("/exports/transactions.csv"));
    }

    internal static async Task VerifyUserIsolation(WebApplicationFactory<Program> app, HttpClient owner)
    {
        await RegisterAndAuthorize(owner, "Proprietário", "owner@fintrack.local");
        var account = await CreateAccount(owner, "Conta privada", 100);
        var category = await CreateCategory(owner, "Categoria privada", CategoryType.Expense);
        var date = new DateOnly(2026, 8, 16);
        var transaction = await CreateTransaction(owner, account.Id, category.Id, TransactionType.Expense, 25, date, "Descrição privada");
        using var other = app.CreateClient();
        await RegisterAndAuthorize(other, "Visitante", "other@fintrack.local");
        var otherAccount = await CreateAccount(other, "Minha conta");
        var otherCategory = await CreateCategory(other, "Minha categoria", CategoryType.Expense);
        var otherTransaction = await CreateTransaction(other, otherAccount.Id, otherCategory.Id, TransactionType.Expense, 1, date, "Meu lançamento");
        var accountEdit = new AccountRequest("Alterada", AccountType.Checking, 999, account.OpeningDate);
        var categoryEdit = new CategoryRequest("Alterada", CategoryType.Expense);
        var transactionEdit = new TransactionRequest(otherAccount.Id, otherCategory.Id, TransactionType.Expense, 999, date, "Alterado");
        foreach (var (url, body) in new (string, object)[]
        {
            ($"/accounts/{account.Id}", accountEdit),
            ($"/categories/{category.Id}", categoryEdit),
            ($"/transactions/{transaction.Id}", transactionEdit)
        })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync(url, body)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync(url)).StatusCode);
        }

        Assert.Equal(otherAccount.Id, Assert.Single((await other.GetFromJsonAsync<List<AccountResponse>>("/accounts"))!).Id);
        Assert.Equal(otherCategory.Id, Assert.Single((await other.GetFromJsonAsync<List<CategoryResponse>>("/categories"))!).Id);
        var filtered = await other.GetFromJsonAsync<TransactionPageResponse>($"/transactions?accountId={account.Id}&categoryId={category.Id}");
        Assert.NotNull(filtered);
        Assert.Empty(filtered.Items);
        Assert.Equal(0, filtered.TotalCount);
        Assert.DoesNotContain("Descrição privada", await other.GetStringAsync("/exports/transactions.csv"));
        var dashboard = await other.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=2026&month=8");
        Assert.NotNull(dashboard);
        Assert.Equal(-1, dashboard.CurrentBalance);
        Assert.Equal(1, dashboard.TotalExpense);
        Assert.Equal("Minha categoria", Assert.Single(dashboard.ExpensesByCategory).CategoryName);

        foreach (var request in new[]
        {
            transactionEdit with { AccountId = account.Id },
            transactionEdit with { CategoryId = category.Id }
        })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await other.PostAsJsonAsync("/transactions", request)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await other.PutAsJsonAsync($"/transactions/{otherTransaction.Id}", request)).StatusCode);
            Assert.Equal(otherTransaction, await other.GetFromJsonAsync<TransactionResponse>($"/transactions/{otherTransaction.Id}"));
        }
        Assert.Equal(account, await owner.GetFromJsonAsync<AccountResponse>($"/accounts/{account.Id}"));
        Assert.Equal(category, await owner.GetFromJsonAsync<CategoryResponse>($"/categories/{category.Id}"));
        Assert.Equal(transaction, await owner.GetFromJsonAsync<TransactionResponse>($"/transactions/{transaction.Id}"));
    }

    internal static async Task VerifyDashboardPeriods(WebApplicationFactory<Program> app, HttpClient client)
    {
        await RegisterAndAuthorize(client);
        var account = await CreateAccount(client, "Conta", 50, new DateOnly(2024, 1, 1));
        await CreateAccount(client, "Futura", 9999, new DateOnly(2024, 3, 1));
        var income = await CreateCategory(client, "Salário", CategoryType.Income);
        var expense = await CreateCategory(client, "Mercado", CategoryType.Expense);
        var sameName = await CreateCategory(client, "Mercado", CategoryType.Expense);
        await CreateTransaction(client, account.Id, income.Id, TransactionType.Income, 100, new DateOnly(2024, 1, 31), null);
        await CreateTransaction(client, account.Id, expense.Id, TransactionType.Expense, 20, new DateOnly(2024, 1, 31), null);
        await CreateTransaction(client, account.Id, income.Id, TransactionType.Income, 200, new DateOnly(2024, 2, 1), null);
        await CreateTransaction(client, account.Id, expense.Id, TransactionType.Expense, 30, new DateOnly(2024, 2, 29), null);
        await CreateTransaction(client, account.Id, expense.Id, TransactionType.Expense, 5, new DateOnly(2024, 2, 29), null);
        await CreateTransaction(client, account.Id, sameName.Id, TransactionType.Expense, 10, new DateOnly(2024, 2, 15), null);
        await CreateTransaction(client, account.Id, income.Id, TransactionType.Income, 999, new DateOnly(2024, 3, 1), null);
        using var other = app.CreateClient();
        await RegisterAndAuthorize(other, "Outro", "outro@email.com");
        var otherAccount = await CreateAccount(other, "Outra conta", 8888, new DateOnly(2024, 1, 1));
        var otherCategory = await CreateCategory(other, "Outra categoria", CategoryType.Expense);
        await CreateTransaction(other, otherAccount.Id, otherCategory.Id, TransactionType.Expense, 888, new DateOnly(2024, 2, 1), null);

        var dashboard = await client.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=2024&month=2");
        Assert.NotNull(dashboard);
        Assert.Equal(200, dashboard.TotalIncome);
        Assert.Equal(45, dashboard.TotalExpense);
        Assert.Equal(155, dashboard.MonthBalance);
        Assert.Equal(285, dashboard.CurrentBalance);
        Assert.Equal(new decimal[] { 35, 10 }, dashboard.ExpensesByCategory.Select(item => item.Total));
        Assert.Equal(4, dashboard.LatestTransactions.Count);
        Assert.All(dashboard.LatestTransactions, item => Assert.Equal(2, item.Date.Month));
        var empty = await client.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=2023&month=12");
        Assert.NotNull(empty);
        Assert.Equal(0, empty.TotalIncome);
        Assert.Equal(0, empty.TotalExpense);
        Assert.Equal(0, empty.CurrentBalance);
        Assert.Empty(empty.ExpensesByCategory);
        Assert.Empty(empty.LatestTransactions);
    }

    internal static async Task VerifyTransactionPages(HttpClient client)
    {
        await RegisterAndAuthorize(client, name: "Paginação", email: "pages@fintrack.local");
        var account = await CreateAccount(client, "Conta paginada");
        var category = await CreateCategory(client, "Categoria paginada", CategoryType.Expense);
        var date = new DateOnly(2026, 8, 16);
        for (var i = 1; i <= 55; i++)
            await CreateTransaction(client, account.Id, category.Id, TransactionType.Expense, i, date, $"Item {i:D2}");

        var first = await client.GetFromJsonAsync<TransactionPageResponse>("/transactions?year=2026&month=8&sort=amount&direction=asc");
        var second = await client.GetFromJsonAsync<TransactionPageResponse>("/transactions?year=2026&month=8&sort=amount&direction=asc&page=2");
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(55, first.TotalCount);
        Assert.Equal(50, first.Items.Count);
        Assert.Equal(1, first.Items[0].Amount);
        Assert.Equal(5, second.Items.Count);
        Assert.Equal(51, second.Items[0].Amount);
        Assert.Empty(first.Items.Select(t => t.Id).Intersect(second.Items.Select(t => t.Id)));
        var empty = await client.GetFromJsonAsync<TransactionPageResponse>("/transactions?page=3");
        Assert.NotNull(empty);
        Assert.Empty(empty.Items);
        Assert.Equal(55, empty.TotalCount);
        foreach (var term in new[] { "item 55", "conta paginada", "categoria paginada", "2026-08-16" })
        {
            var result = await client.GetFromJsonAsync<TransactionPageResponse>($"/transactions?search={Uri.EscapeDataString(term)}&pageSize=1");
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(term == "item 55" ? 1 : 55, result.TotalCount);
        }
        foreach (var sort in new[] { "date", "description", "category", "type", "amount" })
        {
            var response = await client.GetAsync($"/transactions?sort={sort}&direction=desc&pageSize=2&type=Expense&accountId={account.Id}&categoryId={category.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        foreach (var invalid in new[] { "page=0", "page=-1", "pageSize=0", "pageSize=101", "page=2147483647", "sort=invalid", "direction=invalid", "type=999", $"search={new string('a', 161)}" })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/transactions?{invalid}")).StatusCode);
        var dashboard = await client.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=2026&month=8");
        Assert.NotNull(dashboard);
        Assert.Equal(1540, dashboard.TotalExpense);
        var csv = await client.GetStringAsync("/exports/transactions.csv?year=2026&month=8");
        Assert.Equal(56, csv.Trim().Split('\n').Length);
        await RegisterAndAuthorize(client, name: "Outro usuário paginado", email: "other-pages@fintrack.local");
        var other = await client.GetFromJsonAsync<TransactionPageResponse>("/transactions?search=Item");
        Assert.NotNull(other);
        Assert.Empty(other.Items);
        Assert.Equal(0, other.TotalCount);
    }

    internal static async Task<AuthResponse> RegisterAndAuthorize(
        HttpClient client,
        string name = "Bernardo",
        string email = "bernardo@email.com")
    {
        var register = await client.PostAsJsonAsync("/auth/register", new RegisterRequest(name, email, "Senha@123"));
        Assert.True(register.StatusCode == HttpStatusCode.Created, await register.Content.ReadAsStringAsync());

        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        return auth;
    }

    internal static async Task<AccountResponse> CreateAccount(HttpClient client, string name, decimal initialBalance = 0, DateOnly? openingDate = null)
    {
        var response = await client.PostAsJsonAsync("/accounts", new AccountRequest(name, AccountType.Checking, initialBalance, openingDate ?? new DateOnly(2026, 1, 1)));
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(account);

        return account;
    }

    internal static async Task<CategoryResponse> CreateCategory(HttpClient client, string name, CategoryType type)
    {
        var response = await client.PostAsJsonAsync("/categories", new CategoryRequest(name, type));
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(category);

        return category;
    }

    internal static async Task<TransactionResponse> CreateTransaction(
        HttpClient client,
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string? description)
    {
        var response = await client.PostAsJsonAsync("/transactions", new TransactionRequest(accountId, categoryId, type, amount, date, description));
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(transaction);

        return transaction;
    }
}
