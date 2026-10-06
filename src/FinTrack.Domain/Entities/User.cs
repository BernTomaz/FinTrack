namespace FinTrack.Domain.Entities;

public sealed class User
{
    public User(string name, string email, string passwordHash, bool isAdmin = false)
    {
        Name = Required(name);
        Email = Required(email);
        PasswordHash = Required(passwordHash);
        IsAdmin = isAdmin;
    }

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsAdmin { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public void UpdateName(string name)
    {
        Name = Required(name);
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = Required(passwordHash);
        ResetLoginAttempts();
        IsLocked = false;
    }

    public void RegisterFailedLogin(int maxAttempts)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            IsLocked = true;
        }
    }

    public void ResetLoginAttempts()
    {
        FailedLoginAttempts = 0;
    }

    public void ResetBlockedPassword(string passwordHash)
    {
        PasswordHash = Required(passwordHash);
        ResetLoginAttempts();
        IsLocked = false;
    }

    private static string Required(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.") : value.Trim();
}
