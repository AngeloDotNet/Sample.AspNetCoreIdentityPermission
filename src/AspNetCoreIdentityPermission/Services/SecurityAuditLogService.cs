using TestAspNetCoreIdentityPermission.Data;
using TestAspNetCoreIdentityPermission.Models;

namespace TestAspNetCoreIdentityPermission.Services;

public class SecurityAuditLogService : ISecurityAuditLogService
{
	private readonly ApplicationDbContext _db;

	public SecurityAuditLogService(ApplicationDbContext db)
	{
		_db = db;
	}

	public async Task LogAsync(Guid? userId, string eventType, string? ipAddress = null, string? userAgent = null, string? details = null)
	{
		_db.SecurityAuditLogs.Add(new SecurityAuditLog
		{
			Id = Guid.NewGuid(),
			UserId = userId,
			EventType = eventType,
			IpAddress = ipAddress,
			UserAgent = userAgent,
			Details = details,
			CreatedAtUtc = DateTime.UtcNow
		});

		await _db.SaveChangesAsync();
	}
}