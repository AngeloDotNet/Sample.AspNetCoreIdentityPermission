namespace TestAspNetCoreIdentityPermission.DTOs;

public record AuditLogQuery(Guid? UserId, string? EventType, DateTime? FromUtc, DateTime? ToUtc, int Page = 1, int PageSize = 50);