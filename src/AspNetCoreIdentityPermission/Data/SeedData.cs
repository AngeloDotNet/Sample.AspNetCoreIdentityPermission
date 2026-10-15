using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TestAspNetCoreIdentityPermission.Models;

namespace TestAspNetCoreIdentityPermission.Data;

public static class SeedData
{
	public static async Task InitializeAsync(IServiceProvider serviceProvider)
	{
		using var scope = serviceProvider.CreateScope();

		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

		await db.Database.MigrateAsync();

		var roles = new[]
		{
			new { Name = "Admin", Description = "Administrator with full access" },
			new { Name = "Manager", Description = "Manager with delegated access" },
			new { Name = "User", Description = "Standard application user" }
		};

		foreach (var r in roles)
		{
			if (!await roleManager.RoleExistsAsync(r.Name))
			{
				await roleManager.CreateAsync(new ApplicationRole
				{
					Id = Guid.NewGuid(),
					Name = r.Name,
					NormalizedName = r.Name.ToUpperInvariant(),
					Description = r.Description
				});
			}
		}

		var permissionNames = new[]
		{
			("Users.Read", "Read users"),
			("Users.Create", "Create users"),
			("Users.Update", "Update users"),
			("Users.Delete", "Delete users"),
			("Users.AssignPermission", "Assign permissions to users"),
			("Users.RevokeSessions", "Revoke all user sessions"),
			("Roles.Read", "Read roles"),
			("Roles.Create", "Create roles"),
			("Roles.Update", "Update roles"),
			("Roles.Delete", "Delete roles"),
			("Roles.AssignPermission", "Assign permissions to roles"),
			("Permissions.Read", "Read permissions"),
			("Permissions.Create", "Create permissions"),
			("Permissions.Update", "Update permissions"),
			("Permissions.Delete", "Delete permissions"),
			("Reports.View", "View reports")
		};

		foreach (var (name, desc) in permissionNames)
		{
			if (!await db.Permissions.AnyAsync(p => p.Name == name))
			{
				db.Permissions.Add(new Permission
				{
					Id = Guid.NewGuid(),
					Name = name,
					Description = desc
				});
			}
		}

		await db.SaveChangesAsync();

		var permissionMap = await db.Permissions.ToDictionaryAsync(x => x.Name, x => x.Id);

		async Task AssignRolePermission(string roleName, params string[] permissionNamesToAssign)
		{
			var role = await roleManager.FindByNameAsync(roleName);
			if (role is null) return;

			foreach (var permissionName in permissionNamesToAssign)
			{
				var permissionId = permissionMap[permissionName];

				var exists = await db.RolePermissions.AnyAsync(x => x.RoleId == role.Id && x.PermissionId == permissionId);
				if (!exists)
				{
					db.RolePermissions.Add(new RolePermission
					{
						RoleId = role.Id,
						PermissionId = permissionId
					});
				}
			}
		}

		await AssignRolePermission("Admin",
			"Users.Read", "Users.Create", "Users.Update", "Users.Delete",
			"Users.AssignPermission", "Users.RevokeSessions",
			"Roles.Read", "Roles.Create", "Roles.Update", "Roles.Delete", "Roles.AssignPermission",
			"Permissions.Read", "Permissions.Create", "Permissions.Update", "Permissions.Delete",
			"Reports.View");

		await AssignRolePermission("Manager",
			"Users.Read", "Users.Update", "Roles.Read", "Permissions.Read", "Reports.View");

		await AssignRolePermission("User",
			"Reports.View");

		await db.SaveChangesAsync();

		var adminEmail = "admin@yourapp.local";
		var adminPassword = "Admin123!";

		var admin = await userManager.FindByEmailAsync(adminEmail);
		if (admin is null)
		{
			admin = new ApplicationUser
			{
				Id = Guid.NewGuid(),
				UserName = adminEmail,
				Email = adminEmail,
				EmailConfirmed = true,
				FirstName = "System",
				LastName = "Administrator"
			};

			var createResult = await userManager.CreateAsync(admin, adminPassword);
			if (!createResult.Succeeded)
				throw new Exception(string.Join("; ", createResult.Errors.Select(e => e.Description)));

			await userManager.AddToRoleAsync(admin, "Admin");
		}
		else
		{
			var isInRole = await userManager.IsInRoleAsync(admin, "Admin");
			if (!isInRole)
				await userManager.AddToRoleAsync(admin, "Admin");
		}
	}
}