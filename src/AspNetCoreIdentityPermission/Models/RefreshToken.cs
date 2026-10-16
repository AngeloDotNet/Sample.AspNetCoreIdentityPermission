using TestAspNetCoreIdentityPermission.Models.Common;

namespace TestAspNetCoreIdentityPermission.Models;

public class RefreshToken : BaseEntity
{
    //public Guid Id { get; set; }

    public string TokenHash { get; set; } = default!;
    public Guid FamilyId { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? RevocationReason { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public bool IsActive => RevokedAtUtc == null && ExpiresAtUtc > DateTime.UtcNow;
}