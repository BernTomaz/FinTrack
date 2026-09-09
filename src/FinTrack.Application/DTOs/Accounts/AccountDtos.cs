using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Accounts;

public sealed record AccountRequest(string Name, AccountType Type, decimal InitialBalance, DateOnly? OpeningDate = null);

public sealed record AccountResponse(Guid Id, string Name, AccountType Type, decimal InitialBalance, DateOnly OpeningDate, DateTime CreatedAt);
