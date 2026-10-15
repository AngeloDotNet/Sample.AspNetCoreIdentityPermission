namespace TestAspNetCoreIdentityPermission.DTOs;

public record RegisterRequest(
	string Email,
	string Password,
	string? FirstName,
	string? LastName
);