using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinTrack.Application.DTOs.Accounts;
using FinTrack.Application.DTOs.Auth;
using FinTrack.Application.DTOs.Categories;
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

        var updatedAccount = await client.PutAsJsonAsync($"/accounts/{createdAccount.Id}", new AccountRequest("Reserva", AccountType.Savings, 100));
        Assert.Equal(HttpStatusCode.OK, updatedAccount.StatusCode);

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

        var deleteAccount = await client.DeleteAsync($"/accounts/{createdAccount.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteAccount.StatusCode);

        var deleteCategory = await client.DeleteAsync($"/categories/{createdCategory.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCategory.StatusCode);
    }

    [Fact]
    public async Task Auth_returns_expected_errors()
    {
        await using var app = new FinTrackApiFactory();
        using var client = app.CreateClient();

        var badRegister = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("", "", ""));
        Assert.Equal(HttpStatusCode.BadRequest, badRegister.StatusCode);

        await RegisterAndAuthorize(client);

        var duplicate = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("Bernardo", "bernardo@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var badLogin = await client.PostAsJsonAsync("/auth/login", new LoginRequest("", ""));
        Assert.Equal(HttpStatusCode.BadRequest, badLogin.StatusCode);

        var missingPassword = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", ""));
        Assert.Equal(HttpStatusCode.BadRequest, missingPassword.StatusCode);

        var missingUser = await client.PostAsJsonAsync("/auth/login", new LoginRequest("ninguem@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.Unauthorized, missingUser.StatusCode);

        var unauthorized = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "errada"));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        var login = await client.PostAsJsonAsync("/auth/login", new LoginRequest("bernardo@email.com", "Senha@123"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
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

        var deleteCategory = await client.DeleteAsync($"/categories/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, deleteCategory.StatusCode);
    }

    private static async Task<AuthResponse> RegisterAndAuthorize(HttpClient client)
    {
        var register = await client.PostAsJsonAsync("/auth/register", new RegisterRequest("Bernardo", "bernardo@email.com", "Senha@123"));
        Assert.True(register.StatusCode == HttpStatusCode.Created, await register.Content.ReadAsStringAsync());

        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        return auth;
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
