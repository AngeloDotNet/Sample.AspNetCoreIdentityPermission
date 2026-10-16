using Microsoft.AspNetCore.Identity;

namespace TestAspNetCoreIdentityPermission.Models;

public class ApplicationUser : IdentityUser<Guid>
{
	public string? FirstName { get; set; }
	public string? LastName { get; set; }

	public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
	public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}