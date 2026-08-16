using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Transactions;

public sealed record TransactionRequest(
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    DateOnly Date,
    string? Description);

public sealed record TransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid CategoryId,
    TransactionType Type,
    decimal Amount,
    DateOnly Date,
    string? Description,
    DateTime CreatedAt);
