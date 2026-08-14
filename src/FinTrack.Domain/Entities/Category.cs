using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public sealed class Category
{
    public Category(Guid userId, string name, CategoryType type)
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
    }

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public CategoryType Type { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public void Update(string name, CategoryType type)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Name is required.", nameof(name))
            : name.Trim();
        Type = type;
    }
}
