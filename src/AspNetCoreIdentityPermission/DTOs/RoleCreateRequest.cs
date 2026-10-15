namespace TestAspNetCoreIdentityPermission.DTOs;

public record RoleCreateRequest(
	string Name,
	string? Description
);