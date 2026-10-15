using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TestAspNetCoreIdentityPermission.Authorization;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
	public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
		: base(options)
	{
	}

	public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
	{
		if (policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
		{
			var permission = policyName["Permission:".Length..];
			return new AuthorizationPolicyBuilder()
				.AddRequirements(new PermissionRequirement(permission))
				.Build();
		}

		return await base.GetPolicyAsync(policyName);
	}
}