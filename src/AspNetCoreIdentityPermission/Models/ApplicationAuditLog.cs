using TestAspNetCoreIdentityPermission.Models.Common;

namespace TestAspNetCoreIdentityPermission.Models;

public class ApplicationAuditLog : BaseEntity
{
    //public Guid Id { get; set; }

    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string EventType { get; set; } = default!;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}