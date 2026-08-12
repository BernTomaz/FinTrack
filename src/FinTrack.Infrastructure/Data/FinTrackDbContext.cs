using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Data;

public sealed class FinTrackDbContext(DbContextOptions<FinTrackDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Name).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(180).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(account => account.Name).HasMaxLength(120).IsRequired();
            entity.Property(account => account.InitialBalance).HasPrecision(18, 2);
            entity.Property(account => account.Type).HasConversion<string>().HasMaxLength(40);
            entity.HasOne<User>().WithMany().HasForeignKey(account => account.UserId);
            entity.HasIndex(account => new { account.UserId, account.Name });
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).HasMaxLength(120).IsRequired();
            entity.Property(category => category.Type).HasConversion<string>().HasMaxLength(40);
            entity.HasOne<User>().WithMany().HasForeignKey(category => category.UserId);
            entity.HasIndex(category => new { category.UserId, category.Name, category.Type });
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(transaction => transaction.Type).HasConversion<string>().HasMaxLength(40);
            entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.Description).HasMaxLength(300);
            entity.HasOne<User>().WithMany().HasForeignKey(transaction => transaction.UserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Account>().WithMany().HasForeignKey(transaction => transaction.AccountId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Category>().WithMany().HasForeignKey(transaction => transaction.CategoryId).OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(transaction => new { transaction.UserId, transaction.Date });
            entity.HasIndex(transaction => transaction.AccountId);
            entity.HasIndex(transaction => transaction.CategoryId);
        });
    }
}
