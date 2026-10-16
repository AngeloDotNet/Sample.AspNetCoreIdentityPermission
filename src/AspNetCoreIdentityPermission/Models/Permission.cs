using TestAspNetCoreIdentityPermission.Models.Common;

namespace TestAspNetCoreIdentityPermission.Models;

public class Permission : BaseEntity
{
    //public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}