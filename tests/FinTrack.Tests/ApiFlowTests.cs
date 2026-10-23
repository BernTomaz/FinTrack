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

public sealed class ApiFlowTests
{
    [Fact]
    public async Task Transaction_edits_and_deletion_recalculate_exact_balances_across_months()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await VerifyTransactionBalances(client);
    }

    private static async Task VerifyTransactionBalances(HttpClient client)
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

    [Fact]
    public async Task Other_users_cannot_read_change_delete_or_reference_financial_data()
    {
        await using var app = new FinTrackApiFactory();
        using var owner = app.CreateClient();
        await VerifyUserIsolation(app, owner);
    }

    private static async Task VerifyUserIsolation(WebApplicationFactory<Program> app, HttpClient owner)
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
    public void Production_rejects_development_jwt_key()
    {
        using var app = new FinTrackApiFactory().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("Jwt:Key", "dev-only-change-this-key-before-production-123456"));
        Assert.Throws<InvalidOperationException>(() => app.CreateClient());
    }

    [Fact]
    public async Task Production_redirects_to_https_and_only_allows_configured_origin()
    {
        await using var app = new FinTrackApiFactory().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("Jwt:Key", "production-test-key-with-more-than-32-bytes")
            .UseSetting("Cors:AllowedOrigins:0", "https://app.fintrack.test"));
        using var http = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var redirect = await http.GetAsync("/health");
        Assert.Equal(HttpStatusCode.TemporaryRedirect, redirect.StatusCode);
        Assert.Equal("https://localhost/health", redirect.Headers.Location?.ToString());

        using var https = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://fintrack.test") });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://app.fintrack.test");
        var response = await https.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
        Assert.Equal("https://app.fintrack.test", response.Headers.GetValues("Access-Control-Allow-Origin").Single());

        using var deniedRequest = new HttpRequestMessage(HttpMethod.Get, "/health");
        deniedRequest.Headers.Add("Origin", "https://other.fintrack.test");
        var denied = await https.SendAsync(deniedRequest);
        Assert.False(denied.Headers.Contains("Access-Control-Allow-Origin"));
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
    public async Task Maximum_year_includes_last_day_in_dashboard_filters_and_export()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);
        var account = await CreateAccount(client, "Conta", 50, DateOnly.MaxValue);
        var category = await CreateCategory(client, "Categoria", CategoryType.Expense);
        await CreateTransaction(client, account.Id, category.Id, TransactionType.Expense, 10, DateOnly.MaxValue, "Último dia");

        foreach (var filter in new[] { "year=9999", "year=9999&month=12" })
        {
            var transactions = await client.GetFromJsonAsync<TransactionPageResponse>($"/transactions?{filter}");
            Assert.NotNull(transactions);
            Assert.Equal(DateOnly.MaxValue, Assert.Single(transactions.Items).Date);
            var csv = await client.GetAsync($"/exports/transactions.csv?{filter}");
            Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
            Assert.Contains("9999-12-31,Expense,10.00", await csv.Content.ReadAsStringAsync());
        }
        var dashboard = await client.GetFromJsonAsync<MonthlyDashboardResponse>("/dashboard/monthly?year=9999&month=12");
        Assert.NotNull(dashboard);
        Assert.Equal(10, dashboard.TotalExpense);
        Assert.Equal(40, dashboard.CurrentBalance);
    }

    [Fact]
    public async Task Dashboard_preserves_history_month_boundaries_and_user_isolation()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await VerifyDashboardPeriods(app, client);
    }

    private static async Task VerifyDashboardPeriods(WebApplicationFactory<Program> app, HttpClient client)
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

    [Fact]
    public async Task Health_check_returns_database_status_and_correlation_id()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-Id", "test-correlation-id");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("test-correlation-id", response.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Contains("\"database\":\"Healthy\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Auth_accounts_and_categories_flow_works()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();

        await RegisterAndAuthorize(client);

        var me = await client.GetAsync("/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var account = await client.PostAsJsonAsync("/accounts", new AccountRequest("Conta Corrente", AccountType.Checking, 50));
        Assert.True(account.StatusCode == HttpStatusCode.Created, await account.Content.ReadAsStringAsync());
        var createdAccount = await account.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(createdAccount);

        var accounts = await client.GetFromJsonAsync<List<AccountResponse>>("/accounts");
        Assert.NotNull(accounts);
        Assert.Single(accounts);
        Assert.Equal("Conta Corrente", accounts[0].Name);

        var accountById = await client.GetFromJsonAsync<AccountResponse>($"/accounts/{createdAccount.Id}");
        Assert.NotNull(accountById);
        Assert.Equal(createdAccount.Id, accountById.Id);

        var updatedAccount = await client.PutAsJsonAsync($"/accounts/{createdAccount.Id}", new AccountRequest("Reserva", AccountType.Savings, 100, new DateOnly(2026, 8, 1)));
        Assert.Equal(HttpStatusCode.OK, updatedAccount.StatusCode);
        var updatedAccountBody = await updatedAccount.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(updatedAccountBody);
        Assert.Equal("Reserva", updatedAccountBody.Name);
        Assert.Equal(AccountType.Savings, updatedAccountBody.Type);
        Assert.Equal(100, updatedAccountBody.InitialBalance);
        Assert.Equal(new DateOnly(2026, 8, 1), updatedAccountBody.OpeningDate);

        var renamedAccount = await client.PutAsJsonAsync($"/accounts/{createdAccount.Id}", new AccountRequest("Reserva 2", AccountType.Savings, 200));
        Assert.Equal(HttpStatusCode.OK, renamedAccount.StatusCode);
        var renamedAccountBody = await renamedAccount.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(renamedAccountBody);
        Assert.Equal(new DateOnly(2026, 8, 1), renamedAccountBody.OpeningDate);

        var category = await client.PostAsJsonAsync("/categories", new CategoryRequest("Mercado", CategoryType.Expense));
        Assert.True(category.StatusCode == HttpStatusCode.Created, await category.Content.ReadAsStringAsync());
        var createdCategory = await category.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(createdCategory);

        var categories = await client.GetFromJsonAsync<List<CategoryResponse>>("/categories");
        Assert.NotNull(categories);
        Assert.Single(categories);
        Assert.Equal("Mercado", categories[0].Name);

        var categoryById = await client.GetFromJsonAsync<CategoryResponse>($"/categories/{createdCategory.Id}");
        Assert.NotNull(categoryById);
        Assert.Equal(createdCategory.Id, categoryById.Id);

        var updatedCategory = await client.PutAsJsonAsync($"/categories/{createdCategory.Id}", new CategoryRequest("Salário", CategoryType.Income));
        Assert.Equal(HttpStatusCode.OK, updatedCategory.StatusCode);
        var updatedCategoryBody = await updatedCategory.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(updatedCategoryBody);
        Assert.Equal("Salário", updatedCategoryBody.Name);
        Assert.Equal(CategoryType.Income, updatedCategoryBody.Type);

        var deleteAccount = await client.DeleteAsync($"/accounts/{createdAccount.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteAccount.StatusCode);

        var deleteCategory = await client.DeleteAsync($"/categories/{createdCategory.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCategory.StatusCode);
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
    public async Task Dashboard_and_export_flow_works()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);

        var account = await CreateAccount(client, "Conta Corrente", 50);
        var expenseCategory = await CreateCategory(client, "Mercado", CategoryType.Expense);
        var billsCategory = await CreateCategory(client, "Contas", CategoryType.Expense);
        var incomeCategory = await CreateCategory(client, "Salário", CategoryType.Income);

        await CreateTransaction(client, account.Id, incomeCategory.Id, TransactionType.Income, 1000, new DateOnly(2026, 8, 1), "Salário");
        await CreateTransaction(client, account.Id, expenseCategory.Id, TransactionType.Expense, 200, new DateOnly(2026, 8, 2), "Mercado, mês");
        await CreateTransaction(client, account.Id, billsCategory.Id, TransactionType.Expense, 100, new DateOnly(2026, 8, 3), "Conta \"luz\"\nAgosto");

        var dashboardResponse = await client.GetAsync("/dashboard/monthly?year=2026&month=8");
        Assert.True(dashboardResponse.StatusCode == HttpStatusCode.OK, await dashboardResponse.Content.ReadAsStringAsync());

        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<MonthlyDashboardResponse>();
        Assert.NotNull(dashboard);
        Assert.Equal(1000, dashboard.TotalIncome);
        Assert.Equal(300, dashboard.TotalExpense);
        Assert.Equal(700, dashboard.MonthBalance);
        Assert.Equal(750, dashboard.CurrentBalance);
        Assert.Equal(2, dashboard.ExpensesByCategory.Count);
        Assert.Equal("Mercado", dashboard.ExpensesByCategory[0].CategoryName);
        Assert.Equal(3, dashboard.LatestTransactions.Count);

        await CreateTransaction(client, account.Id, billsCategory.Id, TransactionType.Expense, 50, new DateOnly(2026, 8, 4), null);

        var csvResponse = await client.GetAsync($"/exports/transactions.csv?year=2026&month=8&type=Expense&accountId={account.Id}&categoryId={expenseCategory.Id}");
        Assert.Equal(HttpStatusCode.OK, csvResponse.StatusCode);
        Assert.Equal("text/csv", csvResponse.Content.Headers.ContentType?.MediaType);

        var csv = await csvResponse.Content.ReadAsStringAsync();
        Assert.Contains("Date,Type,Amount,Description", csv);
        Assert.Contains("2026-08-02,Expense,200.00,\"Mercado, mês\"", csv);

        var allCsvResponse = await client.GetAsync("/exports/transactions.csv?year=2026&month=8");
        var allCsv = await allCsvResponse.Content.ReadAsStringAsync();
        Assert.Contains("2026-08-04,Expense,50.00,", allCsv);
        Assert.Contains("2026-08-03,Expense,100.00,\"Conta \"\"luz\"\"", allCsv);

        var yearCsvResponse = await client.GetAsync("/exports/transactions.csv?year=2026");
        Assert.Equal(HttpStatusCode.OK, yearCsvResponse.StatusCode);

        var monthCsvResponse = await client.GetAsync("/exports/transactions.csv?month=8");
        Assert.Equal(HttpStatusCode.OK, monthCsvResponse.StatusCode);

        var dateCsvResponse = await client.GetAsync("/exports/transactions.csv?startDate=2026-08-02&endDate=2026-08-03");
        var dateCsv = await dateCsvResponse.Content.ReadAsStringAsync();
        Assert.Contains("2026-08-02,Expense,200.00,\"Mercado, mês\"", dateCsv);
        Assert.Contains("2026-08-03,Expense,100.00,\"Conta \"\"luz\"\"", dateCsv);
        Assert.DoesNotContain("2026-08-01,Income,1000.00,Salário", dateCsv);
    }

    [Fact]
    public async Task Dashboard_and_export_validate_month()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);

        var transactions = await client.GetAsync("/transactions?year=2026&month=13");
        Assert.Equal(HttpStatusCode.BadRequest, transactions.StatusCode);

        var transactionsYear = await client.GetAsync("/transactions?year=10000&month=8");
        Assert.Equal(HttpStatusCode.BadRequest, transactionsYear.StatusCode);

        var dashboard = await client.GetAsync("/dashboard/monthly?year=2026&month=13");
        Assert.Equal(HttpStatusCode.BadRequest, dashboard.StatusCode);

        var dashboardYear = await client.GetAsync("/dashboard/monthly?year=10000&month=8");
        Assert.Equal(HttpStatusCode.BadRequest, dashboardYear.StatusCode);

        var export = await client.GetAsync("/exports/transactions.csv?year=2026&month=13");
        Assert.Equal(HttpStatusCode.BadRequest, export.StatusCode);

        var exportYear = await client.GetAsync("/exports/transactions.csv?year=10000&month=8");
        Assert.Equal(HttpStatusCode.BadRequest, exportYear.StatusCode);

        var exportInvalidRange = await client.GetAsync("/exports/transactions.csv?startDate=2026-08-03&endDate=2026-08-02");
        Assert.Equal(HttpStatusCode.BadRequest, exportInvalidRange.StatusCode);
    }

    [Fact]
    public async Task Auth_returns_expected_errors()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();

        var badRegister = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("", "", ""));
        Assert.Equal(HttpStatusCode.BadRequest, badRegister.StatusCode);

        var invalidEmail = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("Bernardo", "email-invalido", "Senha@123"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidEmail.StatusCode);

        var shortName = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("A", "a@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.BadRequest, shortName.StatusCode);

        var longName = await client.PostAsJsonAsync("/auth/register", new RegisterRequest(new string('A', 81), "longo@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.BadRequest, longName.StatusCode);

        var longEmail = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("Bernardo", $"{new string('a', 121)}@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.BadRequest, longEmail.StatusCode);

        var shortPassword = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("Bernardo", "curta@email.com", "123"));
        Assert.Equal(HttpStatusCode.BadRequest, shortPassword.StatusCode);

        var weakPassword = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("Bernardo", "fraca@email.com", "senha1234"));
        Assert.Equal(HttpStatusCode.BadRequest, weakPassword.StatusCode);

        await RegisterAndAuthorize(client);

        var duplicate = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("Bernardo", "bernardo@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var duplicateName = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("Bernardo", "outro@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.Conflict, duplicateName.StatusCode);

        var badLogin = await client.PostAsJsonAsync("/auth/login", new LoginRequest("", ""));
        Assert.Equal(HttpStatusCode.BadRequest, badLogin.StatusCode);

        var missingPassword = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", ""));
        Assert.Equal(HttpStatusCode.BadRequest, missingPassword.StatusCode);

        var invalidLoginEmail = await client.PostAsJsonAsync("/auth/login", new LoginRequest("email-invalido", "Senha@123"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidLoginEmail.StatusCode);

        var missingUser = await client.PostAsJsonAsync("/auth/login", new LoginRequest("ninguem@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.Unauthorized, missingUser.StatusCode);

        var unauthorized = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "errada"));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        var login = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Login_locks_user_after_three_wrong_passwords()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);

        client.DefaultRequestHeaders.Authorization = null;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "errada"));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var locked = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "errada"));
        Assert.Equal(HttpStatusCode.Locked, locked.StatusCode);
        Assert.Equal("Conta bloqueada por excesso de tentativas. Entre em contato com o administrador para redefinir sua senha.", await locked.Content.ReadAsStringAsync());

        var validPassword = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.Locked, validPassword.StatusCode);
    }

    [Fact]
    public async Task First_registered_user_is_admin()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();

        var first = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("Admin", "admin@email.com", "Senha@123"));
        var firstAuth = await first.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(firstAuth);
        Assert.True(firstAuth.IsAdmin);

        var second = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("Usuario", "usuario@email.com", "Senha@123"));
        var secondAuth = await second.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(secondAuth);
        Assert.False(secondAuth.IsAdmin);
    }

    [Fact]
    public async Task Admin_can_reset_blocked_user_password()
    {
        await using var app = new FinTrackApiFactory();
        using var userClient = app.CreateClient();
        await RegisterAndAuthorize(userClient);
        userClient.DefaultRequestHeaders.Authorization = null;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await userClient.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "errada"));
        }

        var forbidden = await userClient.GetAsync("/auth/admin/locked-users");
        Assert.Equal(HttpStatusCode.Unauthorized, forbidden.StatusCode);

        using var adminClient = app.CreateClient();
        var admin = await RegisterAndAuthorize(adminClient, "Admin", "admin@email.com");

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
            var adminUser = await db.Users.SingleAsync(user => user.Id == admin.UserId);
            db.Entry(adminUser).Property(nameof(FinTrack.Domain.Entities.User.IsAdmin)).CurrentValue = true;
            await db.SaveChangesAsync();
        }

        var adminLogin = await adminClient.PostAsJsonAsync("/auth/login", new LoginRequest("admin@email.com", "Senha@123"));
        var adminAuth = await adminLogin.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(adminAuth);
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.Token);

        var lockedUsers = await adminClient.GetFromJsonAsync<List<LockedUserResponse>>("/auth/admin/locked-users");
        Assert.NotNull(lockedUsers);
        var lockedUser = Assert.Single(lockedUsers);

        var reset = await adminClient.PostAsJsonAsync($"/auth/admin/users/{lockedUser.Id}/reset-password", new AdminResetPasswordRequest("Nova@123"));
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);

        var login = await userClient.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "Nova@123"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Profile_and_password_can_be_updated()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);

        var invalidProfile = await client.PutAsJsonAsync("/auth/me", new UpdateProfileRequest("A"));
        Assert.Equal(HttpStatusCode.BadRequest, invalidProfile.StatusCode);

        var profile = await client.PutAsJsonAsync("/auth/me", new UpdateProfileRequest("Bernardo Tomaz"));
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        var updatedProfile = await profile.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(updatedProfile);
        Assert.Equal("Bernardo Tomaz", updatedProfile.Name);
        Assert.False(string.IsNullOrWhiteSpace(updatedProfile.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", updatedProfile.Token);

        var invalidPassword = await client.PutAsJsonAsync("/auth/password", new ChangePasswordRequest("senha-errada", "Nova@123"));
        Assert.Equal(HttpStatusCode.Unauthorized, invalidPassword.StatusCode);

        var missingPassword = await client.PutAsJsonAsync("/auth/password", new ChangePasswordRequest("", ""));
        Assert.Equal(HttpStatusCode.BadRequest, missingPassword.StatusCode);

        var weakPassword = await client.PutAsJsonAsync("/auth/password", new ChangePasswordRequest("Senha@123", "nova1234"));
        Assert.Equal(HttpStatusCode.BadRequest, weakPassword.StatusCode);

        var password = await client.PutAsJsonAsync("/auth/password", new ChangePasswordRequest("Senha@123", "Nova@123"));
        Assert.Equal(HttpStatusCode.NoContent, password.StatusCode);

        var oldLogin = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "Nova@123"));
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task Profile_rejects_name_used_by_another_user()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await RegisterAndAuthorize(client);

        using var otherClient = app.CreateClient();
        await RegisterAndAuthorize(otherClient, "Ana", "ana@email.com");

        var conflict = await client.PutAsJsonAsync("/auth/me", new UpdateProfileRequest("Ana"));

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Profile_and_password_return_not_found_when_user_no_longer_exists()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        var auth = await RegisterAndAuthorize(client);

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FinTrackDbContext>();
            var user = await db.Users.SingleAsync(user => user.Id == auth.UserId);
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }

        var profile = await client.PutAsJsonAsync("/auth/me", new UpdateProfileRequest("Bernardo Tomaz"));
        Assert.Equal(HttpStatusCode.NotFound, profile.StatusCode);

        var password = await client.PutAsJsonAsync("/auth/password", new ChangePasswordRequest("Senha@123", "Nova@123"));
        Assert.Equal(HttpStatusCode.NotFound, password.StatusCode);
    }

    [Fact]
    public async Task Private_endpoints_and_missing_items_return_expected_status()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/accounts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await RegisterAndAuthorize(client);

        var missingAccount = await client.GetAsync($"/accounts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingAccount.StatusCode);

        var updateAccount = await client.PutAsJsonAsync($"/accounts/{Guid.NewGuid()}", new AccountRequest("Conta", AccountType.Checking, 0));
        Assert.Equal(HttpStatusCode.NotFound, updateAccount.StatusCode);

        var invalidUpdateAccount = await client.PutAsJsonAsync($"/accounts/{Guid.NewGuid()}", new AccountRequest("", AccountType.Checking, 0));
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdateAccount.StatusCode);

        var invalidAccount = await client.PostAsJsonAsync("/accounts", new AccountRequest("", AccountType.Checking, 0));
        Assert.Equal(HttpStatusCode.BadRequest, invalidAccount.StatusCode);

        var shortAccount = await client.PostAsJsonAsync("/accounts", new AccountRequest("A", AccountType.Checking, 0));
        Assert.Equal(HttpStatusCode.BadRequest, shortAccount.StatusCode);

        var longAccount = await client.PostAsJsonAsync("/accounts", new AccountRequest(new string('A', 81), AccountType.Checking, 0));
        Assert.Equal(HttpStatusCode.BadRequest, longAccount.StatusCode);

        var deleteAccount = await client.DeleteAsync($"/accounts/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, deleteAccount.StatusCode);

        var missingCategory = await client.GetAsync($"/categories/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missingCategory.StatusCode);

        var updateCategory = await client.PutAsJsonAsync($"/categories/{Guid.NewGuid()}", new CategoryRequest("Categoria", CategoryType.Expense));
        Assert.Equal(HttpStatusCode.NotFound, updateCategory.StatusCode);

        var invalidUpdateCategory = await client.PutAsJsonAsync($"/categories/{Guid.NewGuid()}", new CategoryRequest("", CategoryType.Expense));
        Assert.Equal(HttpStatusCode.BadRequest, invalidUpdateCategory.StatusCode);

        var invalidCategory = await client.PostAsJsonAsync("/categories", new CategoryRequest("", CategoryType.Expense));
        Assert.Equal(HttpStatusCode.BadRequest, invalidCategory.StatusCode);

        var shortCategory = await client.PostAsJsonAsync("/categories", new CategoryRequest("A", CategoryType.Expense));
        Assert.Equal(HttpStatusCode.BadRequest, shortCategory.StatusCode);

        var deleteCategory = await client.DeleteAsync($"/categories/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, deleteCategory.StatusCode);
    }

    [Fact]
    public async Task Transactions_are_paged_filtered_sorted_and_isolated()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();
        await VerifyTransactionPages(client);
    }

    private static async Task VerifyTransactionPages(HttpClient client)
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

    private static async Task<AuthResponse> RegisterAndAuthorize(
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

    private static async Task<AccountResponse> CreateAccount(HttpClient client, string name, decimal initialBalance = 0, DateOnly? openingDate = null)
    {
        var response = await client.PostAsJsonAsync("/accounts", new AccountRequest(name, AccountType.Checking, initialBalance, openingDate ?? new DateOnly(2026, 1, 1)));
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
        Assert.NotNull(account);

        return account;
    }

    private static async Task<CategoryResponse> CreateCategory(HttpClient client, string name, CategoryType type)
    {
        var response = await client.PostAsJsonAsync("/categories", new CategoryRequest(name, type));
        Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());

        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(category);

        return category;
    }

    private static async Task<TransactionResponse> CreateTransaction(
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

    private sealed class FinTrackApiFactory(string? sqlConnection = null) : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
            if (sqlConnection is not null)
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", sqlConnection);
                return;
            }
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<FinTrackDbContext>>();
                for (var index = services.Count - 1; index >= 0; index--)
                {
                    var descriptor = services[index];
                    if (IsSqlServerProviderService(descriptor))
                    {
                        services.RemoveAt(index);
                    }
                }

                var databaseProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                services.AddDbContext<FinTrackDbContext>(options =>
                    options
                        .UseInMemoryDatabase(_databaseName)
                        .UseInternalServiceProvider(databaseProvider));
            });
        }

        private static bool IsSqlServerProviderService(ServiceDescriptor descriptor) =>
            descriptor.ServiceType.Name == "IDatabaseProvider" ||
            ContainsSqlServer(descriptor.ServiceType) ||
            ContainsSqlServer(descriptor.ImplementationType);

        private static bool ContainsSqlServer(Type? type) =>
            type?.FullName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;
    }
}

internal sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FINTRACK_TEST_SQLSERVER")))
        {
            Skip = "Defina FINTRACK_TEST_SQLSERVER para testar em um banco SQL Server temporário.";
        }
    }
}
