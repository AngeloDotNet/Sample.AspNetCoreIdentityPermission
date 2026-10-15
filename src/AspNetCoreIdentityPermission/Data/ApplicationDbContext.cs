using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TestAspNetCoreIdentityPermission.Models;

namespace TestAspNetCoreIdentityPermission.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{
	}

	public DbSet<Permission> Permissions => Set<Permission>();
	public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
	public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
	public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
	public DbSet<SecurityAuditLog> SecurityAuditLogs => Set<SecurityAuditLog>();
	public DbSet<ApplicationAuditLog> ApplicationAuditLogs => Set<ApplicationAuditLog>();

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		builder.Entity<Permission>(entity =>
		{
			entity.HasIndex(x => x.Name).IsUnique();
			entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
		});

		builder.Entity<RolePermission>(entity =>
		{
			entity.HasKey(x => new { x.RoleId, x.PermissionId });

			entity.HasOne(x => x.Role)
				.WithMany(r => r.RolePermissions)
				.HasForeignKey(x => x.RoleId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.Permission)
				.WithMany(p => p.RolePermissions)
				.HasForeignKey(x => x.PermissionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<UserPermission>(entity =>
		{
			entity.HasKey(x => new { x.UserId, x.PermissionId });

			entity.HasOne(x => x.User)
				.WithMany(u => u.UserPermissions)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasOne(x => x.Permission)
				.WithMany(p => p.UserPermissions)
				.HasForeignKey(x => x.PermissionId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<RefreshToken>(entity =>
		{
			entity.HasIndex(x => x.TokenHash).IsUnique();
			entity.HasIndex(x => x.FamilyId);
			entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
			entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(128);
			entity.Property(x => x.RevocationReason).HasMaxLength(500);

			entity.HasOne(x => x.User)
				.WithMany(u => u.RefreshTokens)
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		builder.Entity<SecurityAuditLog>(entity =>
		{
			entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
			entity.Property(x => x.IpAddress).HasMaxLength(100);
			entity.Property(x => x.UserAgent).HasMaxLength(500);
			entity.Property(x => x.Details).HasMaxLength(2000);

			entity.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.SetNull);
		});

		builder.Entity<ApplicationAuditLog>(entity =>
		{
			entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
			entity.Property(x => x.EntityName).HasMaxLength(200);
			entity.Property(x => x.EntityId).HasMaxLength(100);
			entity.Property(x => x.Details).HasMaxLength(2000);

			entity.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.SetNull);
		});
	}
}