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

public sealed class SqlServerFlowTests
{
    [SqlServerFact]
    public async Task Dashboard_queries_work_on_sql_server()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("FINTRACK_TEST_SQLSERVER"))
        {
            InitialCatalog = $"FinTrackTests_{Guid.NewGuid():N}"
        }.ConnectionString;
        await using var app = new FinTrackApiFactory(connection);
        try
        {
            using var client = app.CreateClient();
            var registrations = await Task.WhenAll(
                client.PostAsJsonAsync("/auth/register", new RegisterRequest("Concorrente A", "parallel-a@fintrack.local", "Senha@123")),
                client.PostAsJsonAsync("/auth/register", new RegisterRequest("Concorrente B", "parallel-b@fintrack.local", "Senha@123")));
            Assert.All(registrations, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
            var users = await Task.WhenAll(registrations.Select(response => response.Content.ReadFromJsonAsync<AuthResponse>()));
            Assert.Single(users, user => user!.IsAdmin);
            await VerifyDashboardPeriods(app, client);
            var accounts = await client.GetFromJsonAsync<List<AccountResponse>>("/accounts");
            var categories = await client.GetFromJsonAsync<List<CategoryResponse>>("/categories");
            Assert.NotNull(accounts);
            Assert.NotNull(categories);
            var account = accounts.Single(item => item.Name == "Conta");
            var category = categories.First(item => item.Type == CategoryType.Expense);
            var accountEdit = await client.PutAsJsonAsync($"/accounts/{account.Id}",
                new AccountRequest(account.Name, account.Type, account.InitialBalance, new DateOnly(2024, 2, 1)));
            Assert.Equal(HttpStatusCode.Conflict, accountEdit.StatusCode);
            var categoryEdit = await client.PutAsJsonAsync($"/categories/{category.Id}",
                new CategoryRequest(category.Name, CategoryType.Income));
            Assert.Equal(HttpStatusCode.Conflict, categoryEdit.StatusCode);
            await VerifyTransactionPages(client);
            await VerifyTransactionBalances(client);
            await VerifyUserIsolation(app, client);
            const decimal maximumAmount = 9999999999999999.99m;
            var limitAccount = await CreateAccount(client, "Limite monetário", -maximumAmount);
            var limitCategory = await CreateCategory(client, "Receita limite", CategoryType.Income);
            var limitTransaction = await CreateTransaction(client, limitAccount.Id, limitCategory.Id,
                TransactionType.Income, maximumAmount, new DateOnly(2026, 8, 16), null);
            Assert.Equal(-maximumAmount, (await client.GetFromJsonAsync<AccountResponse>($"/accounts/{limitAccount.Id}"))!.InitialBalance);
            Assert.Equal(maximumAmount, (await client.GetFromJsonAsync<TransactionResponse>($"/transactions/{limitTransaction.Id}"))!.Amount);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/transactions", new TransactionRequest(
                limitAccount.Id, limitCategory.Id, TransactionType.Income, 1.005m, new DateOnly(2026, 8, 16), null))).StatusCode);
        }
        finally
        {
            await using var db = new FinTrackDbContext(new DbContextOptionsBuilder<FinTrackDbContext>()
                .UseSqlServer(connection).Options);
            await db.Database.EnsureDeletedAsync();
        }
    }
}
