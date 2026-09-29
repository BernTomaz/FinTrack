using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FinTrack.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public void DbContext_exposes_transactions_set()
    {
        var options = new DbContextOptionsBuilder<FinTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new FinTrackDbContext(options);

        Assert.IsAssignableFrom<IQueryable<Transaction>>(db.Transactions);
    }

    [Fact]
    public void DbContext_maps_model_constraints()
    {
        var options = new DbContextOptionsBuilder<FinTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new FinTrackDbContext(options);

        var user = db.Model.FindEntityType(typeof(User))!;
        Assert.Equal(120, user.FindProperty(nameof(User.Name))!.GetMaxLength());
        Assert.Equal(180, user.FindProperty(nameof(User.Email))!.GetMaxLength());
        Assert.True(user.GetIndexes().Single(index => HasProperties(index, nameof(User.Email))).IsUnique);

        var account = db.Model.FindEntityType(typeof(Account))!;
        Assert.Equal(120, account.FindProperty(nameof(Account.Name))!.GetMaxLength());
        Assert.Equal(18, account.FindProperty(nameof(Account.InitialBalance))!.GetPrecision());
        Assert.Equal(2, account.FindProperty(nameof(Account.InitialBalance))!.GetScale());
        Assert.True(account.GetIndexes().Any(index => HasProperties(index, nameof(Account.UserId), nameof(Account.Name))));

        var category = db.Model.FindEntityType(typeof(Category))!;
        Assert.Equal(120, category.FindProperty(nameof(Category.Name))!.GetMaxLength());
        Assert.True(category.GetIndexes().Any(index => HasProperties(index, nameof(Category.UserId), nameof(Category.Name), nameof(Category.Type))));

        var transaction = db.Model.FindEntityType(typeof(Transaction))!;
        Assert.Equal(160, transaction.FindProperty(nameof(Transaction.Description))!.GetMaxLength());
        Assert.Equal(DeleteBehavior.NoAction, transaction.GetForeignKeys().Single(key => HasProperties(key, nameof(Transaction.AccountId))).DeleteBehavior);
        Assert.Equal(DeleteBehavior.NoAction, transaction.GetForeignKeys().Single(key => HasProperties(key, nameof(Transaction.CategoryId))).DeleteBehavior);
        Assert.Equal(DeleteBehavior.NoAction, transaction.GetForeignKeys().Single(key => HasProperties(key, nameof(Transaction.UserId))).DeleteBehavior);
    }

    private static bool HasProperties(IReadOnlyIndex index, params string[] names) =>
        index.Properties.Select(property => property.Name).SequenceEqual(names);

    private static bool HasProperties(IReadOnlyForeignKey key, params string[] names) =>
        key.Properties.Select(property => property.Name).SequenceEqual(names);
}
