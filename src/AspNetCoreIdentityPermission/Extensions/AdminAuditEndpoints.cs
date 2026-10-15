using Microsoft.EntityFrameworkCore;
using TestAspNetCoreIdentityPermission.Data;
using TestAspNetCoreIdentityPermission.DTOs;

namespace TestAspNetCoreIdentityPermission.Extensions;

public static class AdminAuditEndpoints
{
	public static IEndpointRouteBuilder MapAdminAuditEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/admin/audit")
			.RequireAuthorization("Permission:Reports.View");

		group.MapGet("/security", async (ApplicationDbContext db, [AsParameters] AuditLogQuery query) =>
		{
			query = query with
			{
				Page = Math.Max(query.Page, 1),
				PageSize = Math.Clamp(query.PageSize, 1, 200)
			};

			var q = db.SecurityAuditLogs.AsQueryable();

			if (query.UserId.HasValue)
				q = q.Where(x => x.UserId == query.UserId.Value);

			if (!string.IsNullOrWhiteSpace(query.EventType))
				q = q.Where(x => x.EventType == query.EventType);

			if (query.FromUtc.HasValue)
				q = q.Where(x => x.CreatedAtUtc >= query.FromUtc.Value);

			if (query.ToUtc.HasValue)
				q = q.Where(x => x.CreatedAtUtc <= query.ToUtc.Value);

			var items = await q
				.OrderByDescending(x => x.CreatedAtUtc)
				.Skip((query.Page - 1) * query.PageSize)
				.Take(query.PageSize)
				.Select(x => new
				{
					x.Id,
					x.UserId,
					x.EventType,
					x.IpAddress,
					x.UserAgent,
					x.Details,
					x.CreatedAtUtc
				})
				.ToListAsync();

			return Results.Ok(items);
		});

		group.MapGet("/application", async (ApplicationDbContext db, [AsParameters] AuditLogQuery query) =>
		{
			query = query with
			{
				Page = Math.Max(query.Page, 1),
				PageSize = Math.Clamp(query.PageSize, 1, 200)
			};

			var q = db.ApplicationAuditLogs.AsQueryable();

			if (query.UserId.HasValue)
				q = q.Where(x => x.UserId == query.UserId.Value);

			if (!string.IsNullOrWhiteSpace(query.EventType))
				q = q.Where(x => x.EventType == query.EventType);

			if (query.FromUtc.HasValue)
				q = q.Where(x => x.CreatedAtUtc >= query.FromUtc.Value);

			if (query.ToUtc.HasValue)
				q = q.Where(x => x.CreatedAtUtc <= query.ToUtc.Value);

			var items = await q
				.OrderByDescending(x => x.CreatedAtUtc)
				.Skip((query.Page - 1) * query.PageSize)
				.Take(query.PageSize)
				.Select(x => new
				{
					x.Id,
					x.UserId,
					x.EventType,
					x.EntityName,
					x.EntityId,
					x.Details,
					x.CreatedAtUtc
				})
				.ToListAsync();

			return Results.Ok(items);
		});

		return app;
	}
}