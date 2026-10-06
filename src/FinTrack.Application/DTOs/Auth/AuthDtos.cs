namespace FinTrack.Application.DTOs.Auth;

public sealed record RegisterRequest(string Name, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record UpdateProfileRequest(string Name);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record AdminResetPasswordRequest(string NewPassword);

public sealed record LockedUserResponse(Guid Id, string Name, string Email, int FailedLoginAttempts);

public sealed record AuthResponse(Guid UserId, string Name, string Email, bool IsAdmin, string Token);
