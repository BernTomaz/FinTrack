using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public sealed class Account
{
    private Account()
    {
        Name = string.Empty;
    }

    public Account(Guid userId, string name, AccountType type, decimal initialBalance = 0, DateOnly? openingDate = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User is required.", nameof(userId));
        }

        UserId = userId;
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Name is required.", nameof(name))
            : name.Trim();
        Type = type;
        InitialBalance = initialBalance;
        OpeningDate = openingDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public AccountType Type { get; private set; }
    public decimal InitialBalance { get; private set; }
    public DateOnly OpeningDate { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public void Update(string name, AccountType type, decimal initialBalance, DateOnly? openingDate = null)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Name is required.", nameof(name))
            : name.Trim();
        Type = type;
        InitialBalance = initialBalance;
        OpeningDate = openingDate ?? OpeningDate;
    }
}
