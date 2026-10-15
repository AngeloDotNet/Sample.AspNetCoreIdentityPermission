using TestAspNetCoreIdentityPermission.Data;
using TestAspNetCoreIdentityPermission.Models;

namespace TestAspNetCoreIdentityPermission.Services;

public class ApplicationAuditLogService : IApplicationAuditLogService
{
	private readonly ApplicationDbContext _db;

	public ApplicationAuditLogService(ApplicationDbContext db)
	{
		_db = db;
	}

	public async Task LogAsync(Guid? userId, string eventType, string? entityName = null, string? entityId = null, string? details = null)
	{
		_db.ApplicationAuditLogs.Add(new ApplicationAuditLog
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			EventType = eventType,
			EntityName = entityName,
			EntityId = entityId,
			Details = details,
			CreatedAtUtc = DateTime.UtcNow
		});

		await _db.SaveChangesAsync();
	}
}