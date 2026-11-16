namespace FinTrack.Domain.Entities;

public sealed class AuditEvent
{
    private AuditEvent() { }

    public AuditEvent(Guid actorUserId, Guid ownerUserId, string action, string entityType,
        Guid entityId, string? beforeJson, string? afterJson, string correlationId)
    {
        ActorUserId = actorUserId;
        OwnerUserId = ownerUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        BeforeJson = beforeJson;
        AfterJson = afterJson;
        CorrelationId = correlationId;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ActorUserId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; } = DateTime.UtcNow;
    public string? BeforeJson { get; private set; }
    public string? AfterJson { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
}
