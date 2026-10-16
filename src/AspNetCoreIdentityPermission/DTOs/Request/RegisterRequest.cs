namespace TestAspNetCoreIdentityPermission.DTOs.Request;

public record RegisterRequest(string Email, string Password, string? FirstName, string? LastName);