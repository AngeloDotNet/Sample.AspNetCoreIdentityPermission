namespace TestAspNetCoreIdentityPermission.Services;

public interface ISecurityAuditLogService
{
	Task LogAsync(Guid? userId, string eventType, string? ipAddress = null, string? userAgent = null, string? details = null);
}