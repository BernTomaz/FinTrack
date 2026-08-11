namespace FinTrack.Domain.Entities;

public sealed class User
{
    public User(string name, string email, string passwordHash)
    {
        Name = Required(name);
        Email = Required(email);
        PasswordHash = Required(passwordHash);
    }

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    private static string Required(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.") : value.Trim();
}
