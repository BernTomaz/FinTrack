using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public sealed class Transaction
{
    public Transaction(
        Guid userId,
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string? description = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User is required.", nameof(userId));
        }

        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Account is required.", nameof(accountId));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.", nameof(categoryId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        UserId = userId;
        AccountId = accountId;
        CategoryId = categoryId;
        Type = type;
        Amount = amount;
        Date = date;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid CategoryId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public void EnsureCategoryIsCompatible(Category category)
    {
        if (category.UserId != UserId)
        {
            throw new InvalidOperationException("Category belongs to another user.");
        }

        if ((int)category.Type != (int)Type)
        {
            throw new InvalidOperationException("Transaction type must match category type.");
        }
    }

    public void EnsureAccountBelongsToUser(Account account)
    {
        if (account.UserId != UserId)
        {
            throw new InvalidOperationException("Account belongs to another user.");
        }
    }

    public void Update(
        Guid accountId,
        Guid categoryId,
        TransactionType type,
        decimal amount,
        DateOnly date,
        string? description = null)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Account is required.", nameof(accountId));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category is required.", nameof(categoryId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        AccountId = accountId;
        CategoryId = categoryId;
        Type = type;
        Amount = amount;
        Date = date;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
