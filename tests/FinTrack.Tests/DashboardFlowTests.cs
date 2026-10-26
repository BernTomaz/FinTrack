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

public sealed class DashboardFlowTests
{
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
}
