using TestAspNetCoreIdentityPermission.Models;

namespace TestAspNetCoreIdentityPermission.Services;

public interface IJwtTokenService
{
	Task<(string accessToken, string refreshToken, DateTime expiresAtUtc, Guid familyId)> CreateTokensAsync(ApplicationUser user, Guid? familyId = null);
	string GenerateRefreshToken();
	string HashTokenWithHmac(string token);
}