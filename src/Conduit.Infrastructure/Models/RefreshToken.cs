namespace Conduit.Infrastructure.Models;

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    // Groups a token and all of its rotated descendants so an entire lineage can be
    // revoked at once when token reuse (theft) is detected.
    public Guid FamilyId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    // SHA-256 hash of the refresh token. The plaintext value is never persisted.
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? RevokedUtc { get; set; }
}
