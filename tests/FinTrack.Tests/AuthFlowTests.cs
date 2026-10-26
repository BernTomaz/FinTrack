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

public sealed class AuthFlowTests
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
}
