using E_Commerce.API.Data;
using E_Commerce.API.Dtos;
using E_Commerce.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace E_Commerce.API.Endpoints;

public static class AuditLogsEndpoints
{
    public static void MapAuditLogsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/audit-logs")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        group.MapGet("/", async (
            [AsParameters] AuditLogQueryParameters query,
            ECommerceContext dbContext,
            CancellationToken ct) =>
        {
            var logsQuery = dbContext.AuditLogs
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                logsQuery = logsQuery.Where(l => l.Action == query.Action);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityType))
            {
                logsQuery = logsQuery.Where(l => l.EntityType == query.EntityType);
            }

            if (query.FromDate.HasValue)
            {
                logsQuery = logsQuery.Where(l => l.Timestamp >= query.FromDate.Value);
            }

            if (query.ToDate.HasValue)
            {
                logsQuery = logsQuery.Where(l => l.Timestamp <= query.ToDate.Value);
            }

            var totalCount = await logsQuery.CountAsync(ct);

            var items = await logsQuery
                .OrderByDescending(l => l.Timestamp)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(l => new AuditLogDto(
                    l.Id,
                    l.Action,
                    l.EntityType,
                    l.EntityId,
                    l.UserEmail,
                    l.OldValues,
                    l.NewValues,
                    l.Timestamp
                ))
                .ToListAsync(ct);

            return ApiResult.Success(new PagedResult<AuditLogDto>
            {
                Items = items,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount
            });
        });
    }
}
