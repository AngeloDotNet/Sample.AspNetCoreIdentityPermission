namespace TestAspNetCoreIdentityPermission.DTOs;

public record LoginRequest(
	string Email,
	string Password
);