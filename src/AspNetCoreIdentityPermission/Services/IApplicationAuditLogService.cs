namespace TestAspNetCoreIdentityPermission.Services;

public interface IApplicationAuditLogService
{
	Task LogAsync(Guid? userId, string eventType, string? entityName = null, string? entityId = null, string? details = null);
}