namespace TestAspNetCoreIdentityPermission.DTOs;

public record AuthResponse(
	string AccessToken,
	string RefreshToken,
	DateTime ExpiresAtUtc
);