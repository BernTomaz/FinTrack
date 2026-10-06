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

namespace FinTrack.Tests;

public sealed class ApiFlowTests
{
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

        var all = await client.GetFromJsonAsync<List<TransactionResponse>>("/transactions");
        Assert.NotNull(all);
        Assert.Single(all);

        var filtered = await client.GetFromJsonAsync<List<TransactionResponse>>(
            $"/transactions?year=2026&month=8&type=Expense&accountId={account.Id}&categoryId={expenseCategory.Id}");
        Assert.NotNull(filtered);
        Assert.Single(filtered);

        var byYear = await client.GetFromJsonAsync<List<TransactionResponse>>("/transactions?year=2026");
        Assert.NotNull(byYear);
        Assert.Single(byYear);

        var byMonth = await client.GetFromJsonAsync<List<TransactionResponse>>("/transactions?month=8");
        Assert.NotNull(byMonth);
        Assert.Single(byMonth);

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

    private sealed class FinTrackApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging => logging.ClearProviders());
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
