using FinTrack.Domain.Enums;

namespace FinTrack.Application.DTOs.Categories;

public sealed record CategoryRequest(string Name, CategoryType Type);

public sealed record CategoryResponse(Guid Id, string Name, CategoryType Type, DateTime CreatedAt);
