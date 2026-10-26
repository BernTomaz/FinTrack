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

public sealed class UserIsolationFlowTests
{
    [Fact]
    public async Task Other_users_cannot_read_change_delete_or_reference_financial_data()
    {
        await using var app = new FinTrackApiFactory();
        using var owner = app.CreateClient();
        await VerifyUserIsolation(app, owner);
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
}
