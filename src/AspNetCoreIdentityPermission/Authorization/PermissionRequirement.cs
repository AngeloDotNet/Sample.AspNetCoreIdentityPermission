using Microsoft.AspNetCore.Authorization;

namespace TestAspNetCoreIdentityPermission.Authorization;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}