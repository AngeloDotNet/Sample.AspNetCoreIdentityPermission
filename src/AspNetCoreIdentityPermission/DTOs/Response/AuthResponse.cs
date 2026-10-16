namespace TestAspNetCoreIdentityPermission.DTOs.Response;

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAtUtc);