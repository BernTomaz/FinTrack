using System.Net.Http.Json;
using System.Text.Json;
using FinTrack.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using static FinTrack.Tests.ApiTestData;

namespace FinTrack.Tests;

public sealed class DailyCashFlowTests
{
    [Fact]
    public async Task Daily_cash_flow_uses_the_whole_month_and_isolates_users()
    {
        await using var app = new FinTrackApiFactory();
        await VerifyDailyCashFlow(app);
    }

    internal static async Task VerifyDailyCashFlow(WebApplicationFactory<Program> app)
    {
        using var owner = app.CreateClient();
        await RegisterAndAuthorize(owner, "Fluxo diário", "daily-owner@fintrack.local");
        var account = await CreateAccount(owner, "Conta diária", 1000);
        var income = await CreateCategory(owner, "Receita diária", CategoryType.Income);
        var expense = await CreateCategory(owner, "Despesa diária", CategoryType.Expense);
        await CreateTransaction(owner, account.Id, income.Id, TransactionType.Income, 1000, new DateOnly(2026, 8, 1), null);
        for (var i = 0; i < 6; i++)
            await CreateTransaction(owner, account.Id, expense.Id, TransactionType.Expense, 50, new DateOnly(2026, 8, 2), null);
        await CreateTransaction(owner, account.Id, income.Id, TransactionType.Income, 25.75m, new DateOnly(2026, 8, 2), null);
        await CreateTransaction(owner, account.Id, expense.Id, TransactionType.Expense, 4.10m, new DateOnly(2026, 8, 2), null);
        await CreateTransaction(owner, account.Id, expense.Id, TransactionType.Expense, 10, new DateOnly(2026, 8, 5), null);
        await CreateTransaction(owner, account.Id, expense.Id, TransactionType.Expense, 200, new DateOnly(2026, 7, 31), null);
        await CreateTransaction(owner, account.Id, income.Id, TransactionType.Income, 900, new DateOnly(2026, 9, 1), null);

        using var other = app.CreateClient();
        await RegisterAndAuthorize(other, "Outro fluxo", "daily-other@fintrack.local");
        var otherAccount = await CreateAccount(other, "Outra conta");
        var otherExpense = await CreateCategory(other, "Outra despesa", CategoryType.Expense);
        await CreateTransaction(other, otherAccount.Id, otherExpense.Id, TransactionType.Expense, 999, new DateOnly(2026, 8, 2), null);

        using var json = JsonDocument.Parse(await owner.GetStringAsync("/dashboard/monthly?year=2026&month=8"));
        var dashboard = json.RootElement;
        var days = dashboard.GetProperty("dailyCashFlow").EnumerateArray().ToArray();
        Assert.Equal(5, dashboard.GetProperty("latestTransactions").GetArrayLength());
        Assert.Equal(new[] { "2026-08-01", "2026-08-02", "2026-08-05" }, days.Select(day => day.GetProperty("date").GetString()));
        Assert.Equal(304.10m, days[1].GetProperty("expense").GetDecimal());
        Assert.Equal(new[] { 1000m, 721.65m, 711.65m }, days.Select(day => day.GetProperty("balance").GetDecimal()));
        Assert.Equal(dashboard.GetProperty("totalIncome").GetDecimal(), days.Sum(day => day.GetProperty("income").GetDecimal()));
        Assert.Equal(dashboard.GetProperty("totalExpense").GetDecimal(), days.Sum(day => day.GetProperty("expense").GetDecimal()));
        Assert.Equal(dashboard.GetProperty("monthBalance").GetDecimal(), days[^1].GetProperty("balance").GetDecimal());

        using var empty = JsonDocument.Parse(await owner.GetStringAsync("/dashboard/monthly?year=2024&month=2"));
        Assert.Empty(empty.RootElement.GetProperty("dailyCashFlow").EnumerateArray());
    }
}
