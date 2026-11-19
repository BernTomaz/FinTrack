namespace FinTrack.Application.DTOs.Audit;

public sealed record AuditEventResponse(Guid Id, Guid ActorUserId, string Action,
    string EntityType, Guid EntityId, DateTime OccurredAtUtc);

public sealed record AuditEventDetailResponse(Guid Id, Guid ActorUserId, string Action,
    string EntityType, Guid EntityId, DateTime OccurredAtUtc, string CorrelationId,
    string? BeforeJson, string? AfterJson);

public sealed record AuditEventPageResponse(IReadOnlyList<AuditEventResponse> Items,
    int TotalCount, int Page, int PageSize);
