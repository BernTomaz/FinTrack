using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;

namespace FinTrack.Tests;

public class DomainTests
{
    [Fact]
    public void User_trims_required_values()
    {
        var user = new User(" Bernardo ", " email@teste.com ", " hash ");

        Assert.Equal("Bernardo", user.Name);
        Assert.Equal("email@teste.com", user.Email);
        Assert.Equal("hash", user.PasswordHash);
    }

    [Fact]
    public void User_requires_values()
    {
        Assert.Throws<ArgumentException>(() => new User("", "email@teste.com", "hash"));
        Assert.Throws<ArgumentException>(() => new User("Bernardo", "", "hash"));
        Assert.Throws<ArgumentException>(() => new User("Bernardo", "email@teste.com", ""));
    }

    [Fact]
    public void Account_requires_user()
    {
        Assert.Throws<ArgumentException>(() => new Account(Guid.Empty, "Conta", AccountType.Checking));
    }

    [Fact]
    public void Account_requires_name()
    {
        Assert.Throws<ArgumentException>(() => new Account(Guid.NewGuid(), "", AccountType.Checking));
    }

    [Fact]
    public void Account_trims_name_and_updates_values()
    {
        var account = new Account(Guid.NewGuid(), " Conta ", AccountType.Checking);

        account.Update(" Reserva ", AccountType.Savings, 100);

        Assert.Equal("Reserva", account.Name);
        Assert.Equal(AccountType.Savings, account.Type);
        Assert.Equal(100, account.InitialBalance);
    }

    [Fact]
    public void Account_update_requires_name()
    {
        var account = new Account(Guid.NewGuid(), "Conta", AccountType.Checking);

        Assert.Throws<ArgumentException>(() => account.Update("", AccountType.Savings, 10));
    }

    [Fact]
    public void Category_requires_user()
    {
        Assert.Throws<ArgumentException>(() => new Category(Guid.Empty, "Mercado", CategoryType.Expense));
    }

    [Fact]
    public void Category_requires_name()
    {
        Assert.Throws<ArgumentException>(() => new Category(Guid.NewGuid(), "", CategoryType.Expense));
    }

    [Fact]
    public void Category_trims_name_and_updates_values()
    {
        var category = new Category(Guid.NewGuid(), " Mercado ", CategoryType.Expense);

        category.Update(" Salário ", CategoryType.Income);

        Assert.Equal("Salário", category.Name);
        Assert.Equal(CategoryType.Income, category.Type);
    }

    [Fact]
    public void Category_update_requires_name()
    {
        var category = new Category(Guid.NewGuid(), "Mercado", CategoryType.Expense);

        Assert.Throws<ArgumentException>(() => category.Update("", CategoryType.Expense));
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
    public void Transaction_requires_references()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(DateTime.Today);

        Assert.Throws<ArgumentException>(() => new Transaction(Guid.Empty, accountId, categoryId, TransactionType.Expense, 10, date));
        Assert.Throws<ArgumentException>(() => new Transaction(userId, Guid.Empty, categoryId, TransactionType.Expense, 10, date));
        Assert.Throws<ArgumentException>(() => new Transaction(userId, accountId, Guid.Empty, TransactionType.Expense, 10, date));
    }

    [Fact]
    public void Transaction_trims_empty_description_to_null()
    {
        var transaction = new Transaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Expense,
            10,
            DateOnly.FromDateTime(DateTime.Today),
            "   ");

        Assert.Null(transaction.Description);
    }

    [Fact]
    public void Transaction_trims_description()
    {
        var transaction = new Transaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Expense,
            10,
            DateOnly.FromDateTime(DateTime.Today),
            " Mercado ");

        Assert.Equal("Mercado", transaction.Description);
    }

    [Fact]
    public void Transaction_accepts_matching_account_and_category()
    {
        var userId = Guid.NewGuid();
        var account = new Account(userId, "Conta", AccountType.Checking);
        var category = new Category(userId, "Mercado", CategoryType.Expense);
        var transaction = new Transaction(
            userId,
            account.Id,
            category.Id,
            TransactionType.Expense,
            10,
            DateOnly.FromDateTime(DateTime.Today));

        transaction.EnsureAccountBelongsToUser(account);
        transaction.EnsureCategoryIsCompatible(category);

        Assert.Equal(account.Id, transaction.AccountId);
        Assert.Equal(category.Id, transaction.CategoryId);
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

    [Fact]
    public void Transaction_rejects_category_from_another_user()
    {
        var category = new Category(Guid.NewGuid(), "Mercado", CategoryType.Expense);
        var transaction = new Transaction(
            Guid.NewGuid(),
            Guid.NewGuid(),
            category.Id,
            TransactionType.Expense,
            10,
            DateOnly.FromDateTime(DateTime.Today));

        Assert.Throws<InvalidOperationException>(() => transaction.EnsureCategoryIsCompatible(category));
    }
}
