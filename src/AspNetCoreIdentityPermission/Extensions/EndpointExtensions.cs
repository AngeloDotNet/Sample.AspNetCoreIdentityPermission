using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TestAspNetCoreIdentityPermission.Data;
using TestAspNetCoreIdentityPermission.DTOs;
using TestAspNetCoreIdentityPermission.Models;
using TestAspNetCoreIdentityPermission.Services;

namespace TestAspNetCoreIdentityPermission.Extensions;

public static class EndpointExtensions
{
	public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/auth");

		group.MapPost("/register", async (
			RegisterRequest request,
			UserManager<ApplicationUser> userManager,
			IJwtTokenService tokenService,
			ISecurityAuditLogService securityAudit,
			HttpContext httpContext) =>
		{
			var user = new ApplicationUser
			{
				Id = Guid.NewGuid(),
				UserName = request.Email,
				Email = request.Email,
				FirstName = request.FirstName,
				LastName = request.LastName,
				EmailConfirmed = true
			};

			var result = await userManager.CreateAsync(user, request.Password);
			if (!result.Succeeded)
				return Results.BadRequest(result.Errors);

			var roleResult = await userManager.AddToRoleAsync(user, "User");
			if (!roleResult.Succeeded)
				return Results.BadRequest(roleResult.Errors);

			var tokens = await tokenService.CreateTokensAsync(user);

			await securityAudit.LogAsync(
				user.Id,
				"RegisterSuccess",
				httpContext.Connection.RemoteIpAddress?.ToString(),
				httpContext.Request.Headers.UserAgent.ToString(),
				"User registered successfully");

			return Results.Ok(new AuthResponse(tokens.accessToken, tokens.refreshToken, tokens.expiresAtUtc));
		});

		group.MapPost("/login", async (
			LoginRequest request,
			UserManager<ApplicationUser> userManager,
			SignInManager<ApplicationUser> signInManager,
			IJwtTokenService tokenService,
			ISecurityAuditLogService securityAudit,
			HttpContext httpContext) =>
		{
			var user = await userManager.FindByEmailAsync(request.Email);
			if (user is null)
			{
				await securityAudit.LogAsync(
					null,
					"LoginFailed",
					httpContext.Connection.RemoteIpAddress?.ToString(),
					httpContext.Request.Headers.UserAgent.ToString(),
					"User not found");

				return Results.Unauthorized();
			}

			var check = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
			if (!check.Succeeded)
			{
				await securityAudit.LogAsync(
					user.Id,
					"LoginFailed",
					httpContext.Connection.RemoteIpAddress?.ToString(),
					httpContext.Request.Headers.UserAgent.ToString(),
					"Invalid password or locked out");

				return Results.Unauthorized();
			}

			var tokens = await tokenService.CreateTokensAsync(user);

			await securityAudit.LogAsync(
				user.Id,
				"LoginSuccess",
				httpContext.Connection.RemoteIpAddress?.ToString(),
				httpContext.Request.Headers.UserAgent.ToString(),
				"Login successful");

			return Results.Ok(new AuthResponse(tokens.accessToken, tokens.refreshToken, tokens.expiresAtUtc));
		});

		group.MapPost("/refresh", async (
			RefreshTokenRequest request,
			ApplicationDbContext db,
			IJwtTokenService tokenService,
			ISecurityAuditLogService securityAudit,
			HttpContext httpContext) =>
		{
			var incomingHash = tokenService.HashTokenWithHmac(request.RefreshToken);

			var token = await db.RefreshTokens
				.Include(x => x.User)
				.FirstOrDefaultAsync(x => x.TokenHash == incomingHash);

			if (token is null)
			{
				await securityAudit.LogAsync(
					null,
					"RefreshFailed",
					httpContext.Connection.RemoteIpAddress?.ToString(),
					httpContext.Request.Headers.UserAgent.ToString(),
					"Invalid refresh token");

				return Results.Unauthorized();
			}

			if (!token.IsActive)
			{
				var familyId = token.FamilyId;

				var compromisedTokens = await db.RefreshTokens
					.Where(x => x.FamilyId == familyId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
					.ToListAsync();

				foreach (var compromised in compromisedTokens)
				{
					compromised.RevokedAtUtc = DateTime.UtcNow;
					compromised.RevocationReason = "Compromised session due to refresh token reuse";
				}

				await db.SaveChangesAsync();

				await securityAudit.LogAsync(
					token.UserId,
					"RefreshTokenReuseDetected",
					httpContext.Connection.RemoteIpAddress?.ToString(),
					httpContext.Request.Headers.UserAgent.ToString(),
					$"Reuse detected for family {familyId}. Active tokens revoked.");

				return Results.Unauthorized();
			}

			token.RevokedAtUtc = DateTime.UtcNow;
			token.RevocationReason = "Rotated on refresh";

			var newTokens = await tokenService.CreateTokensAsync(token.User, token.FamilyId);
			token.ReplacedByTokenHash = tokenService.HashTokenWithHmac(newTokens.refreshToken);

			await db.SaveChangesAsync();

			await securityAudit.LogAsync(
				token.UserId,
				"RefreshSuccess",
				httpContext.Connection.RemoteIpAddress?.ToString(),
				httpContext.Request.Headers.UserAgent.ToString(),
				"Refresh token rotated");

			return Results.Ok(new AuthResponse(newTokens.accessToken, newTokens.refreshToken, newTokens.expiresAtUtc));
		});

		group.MapPost("/revoke-session", async (
			RevokeFamilyRequest request,
			ClaimsPrincipal principal,
			ApplicationDbContext db,
			ISecurityAuditLogService securityAudit,
			HttpContext httpContext) =>
		{
			var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
			if (!Guid.TryParse(userIdValue, out var userId))
				return Results.Unauthorized();

			var tokens = await db.RefreshTokens
				.Where(x => x.UserId == userId && x.FamilyId == request.FamilyId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
				.ToListAsync();

			if (tokens.Count == 0)
				return Results.NotFound("Session not found or already revoked.");

			foreach (var token in tokens)
			{
				token.RevokedAtUtc = DateTime.UtcNow;
				token.RevocationReason = "User revoked session";
			}

			await db.SaveChangesAsync();

			await securityAudit.LogAsync(
				userId,
				"RevokeSession",
				httpContext.Connection.RemoteIpAddress?.ToString(),
				httpContext.Request.Headers.UserAgent.ToString(),
				$"Revoked session family {request.FamilyId}");

			return Results.Ok(new { Revoked = tokens.Count, request.FamilyId });
		}).RequireAuthorization();

		group.MapPost("/revoke-all", async (
			ClaimsPrincipal principal,
			ApplicationDbContext db,
			ISecurityAuditLogService securityAudit,
			HttpContext httpContext) =>
		{
			var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
			if (!Guid.TryParse(userIdValue, out var userId))
				return Results.Unauthorized();

			var activeTokens = await db.RefreshTokens
				.Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
				.ToListAsync();

			foreach (var token in activeTokens)
			{
				token.RevokedAtUtc = DateTime.UtcNow;
				token.RevocationReason = "User requested revoke all";
			}

			await db.SaveChangesAsync();

			await securityAudit.LogAsync(
				userId,
				"RevokeAllRefreshTokens",
				httpContext.Connection.RemoteIpAddress?.ToString(),
				httpContext.Request.Headers.UserAgent.ToString(),
				$"Revoked {activeTokens.Count} active refresh tokens");

			return Results.Ok(new { Revoked = activeTokens.Count });
		}).RequireAuthorization();

		return app;
	}

	public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
	{
		var group = app.MapGroup("/admin")
			.RequireAuthorization();

		group.MapPost("/roles", async (
			RoleCreateRequest request,
			RoleManager<ApplicationRole> roleManager) =>
		{
			var role = new ApplicationRole
			{
				Id = Guid.NewGuid(),
				Name = request.Name,
				NormalizedName = request.Name.ToUpperInvariant(),
				Description = request.Description
			};

			var result = await roleManager.CreateAsync(role);
			return result.Succeeded ? Results.Ok(role) : Results.BadRequest(result.Errors);
		}).RequireAuthorization("Permission:Roles.Create");

		group.MapPost("/permissions", async (
			PermissionCreateRequest request,
			ApplicationDbContext db) =>
		{
			if (await db.Permissions.AnyAsync(x => x.Name == request.Name))
				return Results.Conflict("Permission already exists.");

			var permission = new Permission
			{
				Id = Guid.NewGuid(),
				Name = request.Name,
				Description = request.Description
			};

			db.Permissions.Add(permission);
			await db.SaveChangesAsync();

			return Results.Ok(permission);
		}).RequireAuthorization("Permission:Permissions.Create");

		group.MapPost("/roles/{roleId:guid}/permissions", async (
			Guid roleId,
			AssignPermissionRequest request,
			ApplicationDbContext db) =>
		{
			var roleExists = await db.Roles.AnyAsync(r => r.Id == roleId);
			if (!roleExists)
				return Results.NotFound("Role not found.");

			var permissionExists = await db.Permissions.AnyAsync(p => p.Id == request.PermissionId);
			if (!permissionExists)
				return Results.NotFound("Permission not found.");

			var exists = await db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == request.PermissionId);
			if (exists)
				return Results.Conflict("Mapping already exists.");

			db.RolePermissions.Add(new RolePermission
			{
				RoleId = roleId,
				PermissionId = request.PermissionId
			});

			await db.SaveChangesAsync();
			return Results.Ok();
		}).RequireAuthorization("Permission:Roles.AssignPermission");

		group.MapPost("/users/{userId:guid}/permissions", async (
			Guid userId,
			AssignPermissionRequest request,
			ApplicationDbContext db) =>
		{
			var userExists = await db.Users.AnyAsync(u => u.Id == userId);
			if (!userExists)
				return Results.NotFound("User not found.");

			var permissionExists = await db.Permissions.AnyAsync(p => p.Id == request.PermissionId);
			if (!permissionExists)
				return Results.NotFound("Permission not found.");

			var exists = await db.UserPermissions.AnyAsync(x => x.UserId == userId && x.PermissionId == request.PermissionId);
			if (exists)
				return Results.Conflict("Mapping already exists.");

			db.UserPermissions.Add(new UserPermission
			{
				UserId = userId,
				PermissionId = request.PermissionId
			});

			await db.SaveChangesAsync();
			return Results.Ok();
		}).RequireAuthorization("Permission:Users.AssignPermission");

		group.MapPost("/users/{userId:guid}/revoke-sessions", async (
			Guid userId,
			ApplicationDbContext db,
			ISecurityAuditLogService securityAudit,
			HttpContext httpContext) =>
		{
			var activeTokens = await db.RefreshTokens
				.Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow)
				.ToListAsync();

			foreach (var token in activeTokens)
			{
				token.RevokedAtUtc = DateTime.UtcNow;
				token.RevocationReason = "Admin revoked all sessions for user";
			}

			await db.SaveChangesAsync();

			await securityAudit.LogAsync(
				userId,
				"AdminRevokeAllSessions",
				httpContext.Connection.RemoteIpAddress?.ToString(),
				httpContext.Request.Headers.UserAgent.ToString(),
				$"Admin revoked {activeTokens.Count} sessions for user {userId}");

			return Results.Ok(new { UserId = userId, Revoked = activeTokens.Count });
		}).RequireAuthorization("Permission:Users.RevokeSessions");

		group.MapGet("/me", (ClaimsPrincipal user) =>
		{
			return Results.Ok(new
			{
				UserId = user.FindFirst("sub")?.Value,
				Email = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value,
				Name = user.Identity?.Name,
				Roles = user.FindAll(ClaimTypes.Role).Select(x => x.Value),
				Permissions = user.FindAll("permission").Select(x => x.Value)
			});
		}).RequireAuthorization();

		return app;
	}
}