using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;

namespace FinTrack.Tests;

public class DomainTests
{
    [Fact]
    public void Account_requires_name()
    {
        Assert.Throws<ArgumentException>(() => new Account(Guid.NewGuid(), "", AccountType.Checking));
    }

    [Fact]
    public void Transaction_requires_positive_amount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Transaction(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                TransactionType.Expense,
                0,
                DateOnly.FromDateTime(DateTime.Today)));
    }

    [Fact]
    public void Transaction_rejects_account_from_another_user()
    {
        var userId = Guid.NewGuid();
        var account = new Account(Guid.NewGuid(), "Conta", AccountType.Checking);
        var transaction = new Transaction(
            userId,
            account.Id,
            Guid.NewGuid(),
            TransactionType.Expense,
            10,
            DateOnly.FromDateTime(DateTime.Today));

        Assert.Throws<InvalidOperationException>(() => transaction.EnsureAccountBelongsToUser(account));
    }

    [Fact]
    public void Transaction_rejects_incompatible_category_type()
    {
        var userId = Guid.NewGuid();
        var category = new Category(userId, "Salário", CategoryType.Income);
        var transaction = new Transaction(
            userId,
            Guid.NewGuid(),
            category.Id,
            TransactionType.Expense,
            10,
            DateOnly.FromDateTime(DateTime.Today));

        Assert.Throws<InvalidOperationException>(() => transaction.EnsureCategoryIsCompatible(category));
    }
}
