namespace TestAspNetCoreIdentityPermission.DTOs;

public record PermissionCreateRequest(
	string Name,
	string? Description
);