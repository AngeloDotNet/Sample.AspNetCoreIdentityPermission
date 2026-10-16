using Microsoft.AspNetCore.Identity;

namespace TestAspNetCoreIdentityPermission.Models;

public class ApplicationRole : IdentityRole<Guid>
{
	public string? Description { get; set; }
	public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}