using System.Security.Claims;
using System.Globalization;
using FinTrack.Api.Auth;
using FinTrack.Application.DTOs.Audit;
using FinTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Api.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/audit-events").WithTags("Audit").RequireAuthorization();
        group.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return await next(context);
        });

        group.MapGet("/", async (string? entityType, string? entityId, string? action,
            string? from, string? to, string? page, string? pageSize, FinTrackDbContext db,
            ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var currentPage = 1;
            var size = 20;
            if ((page is not null && !int.TryParse(page, out currentPage)) ||
                (pageSize is not null && !int.TryParse(pageSize, out size)))
                return Results.BadRequest("Invalid pagination.");
            if (currentPage < 1 || size is < 1 or > 100 || (long)(currentPage - 1) * size > int.MaxValue)
                return Results.BadRequest("Page must be positive and pageSize between 1 and 100.");
            if (entityType is not (null or "Account" or "Category" or "Transaction" or "User"))
                return Results.BadRequest("Invalid entity type.");
            if (action is not (null or "Created" or "Updated" or "Deleted" or "PasswordChanged" or "AdminPasswordReset"))
                return Results.BadRequest("Invalid audit action.");
            Guid? recordId = null;
            if (entityId is not null)
            {
                if (!Guid.TryParse(entityId, out var parsedId)) return Results.BadRequest("Invalid entity id.");
                recordId = parsedId;
            }
            DateOnly? fromDate = null, toDate = null;
            if (from is not null)
            {
                if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom))
                    return Results.BadRequest("Invalid from date.");
                fromDate = parsedFrom;
            }
            if (to is not null)
            {
                if (!DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo))
                    return Results.BadRequest("Invalid to date.");
                toDate = parsedTo;
            }
            if (fromDate > toDate)
                return Results.BadRequest("From must not be after to.");

            var userId = user.GetUserId();
            var query = db.AuditEvents.AsNoTracking().Where(audit => audit.OwnerUserId == userId);
            if (entityType is not null) query = query.Where(audit => audit.EntityType == entityType);
            if (recordId is not null) query = query.Where(audit => audit.EntityId == recordId);
            if (action is not null) query = query.Where(audit => audit.Action == action);
            if (fromDate is not null)
            {
                var start = fromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(audit => audit.OccurredAtUtc >= start);
            }
            if (toDate is not null)
            {
                var end = toDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                query = query.Where(audit => audit.OccurredAtUtc <= end);
            }
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(audit => audit.OccurredAtUtc).ThenBy(audit => audit.Id)
                .Skip((currentPage - 1) * size).Take(size)
                .Select(audit => new AuditEventResponse(audit.Id, audit.ActorUserId, audit.Action,
                    audit.EntityType, audit.EntityId, DateTime.SpecifyKind(audit.OccurredAtUtc, DateTimeKind.Utc)))
                .ToListAsync(cancellationToken);
            return Results.Ok(new AuditEventPageResponse(items, total, currentPage, size));
        });

        group.MapGet("/{id:guid}", async (Guid id, FinTrackDbContext db, ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            var audit = await db.AuditEvents.AsNoTracking()
                .Where(audit => audit.Id == id && audit.OwnerUserId == userId)
                .Select(audit => new AuditEventDetailResponse(audit.Id, audit.ActorUserId, audit.Action,
                    audit.EntityType, audit.EntityId, DateTime.SpecifyKind(audit.OccurredAtUtc, DateTimeKind.Utc),
                    audit.CorrelationId, audit.BeforeJson, audit.AfterJson))
                .SingleOrDefaultAsync(cancellationToken);
            return audit is null ? Results.NotFound() : Results.Ok(audit);
        });
        return app;
    }
}
