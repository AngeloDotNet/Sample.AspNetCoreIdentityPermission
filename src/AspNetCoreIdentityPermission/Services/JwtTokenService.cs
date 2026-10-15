using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TestAspNetCoreIdentityPermission.Data;
using TestAspNetCoreIdentityPermission.Models;

namespace TestAspNetCoreIdentityPermission.Services;

public class JwtTokenService : IJwtTokenService
{
	private readonly IConfiguration _configuration;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly ApplicationDbContext _db;

	public JwtTokenService(
		IConfiguration configuration,
		UserManager<ApplicationUser> userManager,
		ApplicationDbContext db)
	{
		_configuration = configuration;
		_userManager = userManager;
		_db = db;
	}

	public async Task<(string accessToken, string refreshToken, DateTime expiresAtUtc, Guid familyId)> CreateTokensAsync(ApplicationUser user, Guid? familyId = null)
	{
		var jwt = _configuration.GetSection("Jwt");
		var issuer = jwt["Issuer"]!;
		var audience = jwt["Audience"]!;
		var key = jwt["Key"]!;
		var minutes = int.Parse(jwt["AccessTokenMinutes"] ?? "15");
		var refreshDays = int.Parse(jwt["RefreshTokenDays"] ?? "7");

		var roles = await _userManager.GetRolesAsync(user);
		var permissions = await GetUserPermissionsAsync(user.Id);

		var claims = new List<Claim>
		{
			new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
			new(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty)
		};

		claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
		claims.AddRange(permissions.Select(p => new Claim("permission", p)));

		var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
		var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

		var expiresAtUtc = DateTime.UtcNow.AddMinutes(minutes);

		var token = new JwtSecurityToken(
			issuer: issuer,
			audience: audience,
			claims: claims,
			expires: expiresAtUtc,
			signingCredentials: creds);

		var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
		var refreshToken = GenerateRefreshToken();
		var refreshTokenHash = HashTokenWithHmac(refreshToken);
		var finalFamilyId = familyId ?? Guid.NewGuid();

		_db.RefreshTokens.Add(new RefreshToken
		{
			Id = Guid.NewGuid(),
			TokenHash = refreshTokenHash,
			FamilyId = finalFamilyId,
			CreatedAtUtc = DateTime.UtcNow,
			ExpiresAtUtc = DateTime.UtcNow.AddDays(refreshDays),
			UserId = user.Id
		});

		await _db.SaveChangesAsync();

		return (accessToken, refreshToken, expiresAtUtc, finalFamilyId);
	}

	public string GenerateRefreshToken()
	{
		var bytes = RandomNumberGenerator.GetBytes(64);
		return Convert.ToBase64String(bytes);
	}

	public string HashTokenWithHmac(string token)
	{
		var secret = _configuration["Jwt:RefreshTokenHashKey"]!;
		var keyBytes = Encoding.UTF8.GetBytes(secret);

		using var hmac = new HMACSHA256(keyBytes);
		var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(token));
		return Convert.ToHexString(hash);
	}

	private async Task<List<string>> GetUserPermissionsAsync(Guid userId)
	{
		var directPermissions = _db.UserPermissions
			.Where(x => x.UserId == userId)
			.Select(x => x.Permission.Name);

		var rolePermissions = from ur in _db.UserRoles
							  join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
							  join p in _db.Permissions on rp.PermissionId equals p.Id
							  where ur.UserId == userId
							  select p.Name;

		return await directPermissions
			.Union(rolePermissions)
			.Distinct()
			.ToListAsync();
	}
}