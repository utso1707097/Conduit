namespace Conduit.Infrastructure.Models;

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresUtc { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime? RevokedUtc { get; set; }
}
