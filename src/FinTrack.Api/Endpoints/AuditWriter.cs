using FinTrack.Api.Auth;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Data;

namespace FinTrack.Api.Endpoints;

internal static class AuditWriter
{
    // Only adds to the current unit of work; the endpoint saves both changes together.
    internal static void Record(FinTrackDbContext db, HttpContext context, Guid ownerUserId,
        string entityType, Guid entityId, string action, string? beforeJson, string? afterJson)
    {
        var correlationId = context.Response.Headers["X-Correlation-Id"].ToString();
        if (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > 128)
            correlationId = context.TraceIdentifier;
        db.AuditEvents.Add(new AuditEvent(context.User.GetUserId(), ownerUserId, action,
            entityType, entityId, beforeJson, afterJson, correlationId));
    }
}
