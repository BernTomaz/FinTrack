using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public sealed class Account
{
    public Account(Guid userId, string name, AccountType type, decimal initialBalance = 0)
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
    }

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public AccountType Type { get; private set; }
    public decimal InitialBalance { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public void Update(string name, AccountType type, decimal initialBalance)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Name is required.", nameof(name))
            : name.Trim();
        Type = type;
        InitialBalance = initialBalance;
    }
}
