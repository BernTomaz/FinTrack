using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
}
