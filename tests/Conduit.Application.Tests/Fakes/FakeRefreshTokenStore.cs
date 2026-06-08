using Conduit.Application.Common;
using Conduit.Application.Users;

namespace Conduit.Application.Tests.Fakes;

internal sealed class FakeRefreshTokenStore : IRefreshTokenStore
{
    private sealed class Entry
    {
        public required string Token { get; init; }
        public required string UserId { get; init; }
        public required Guid FamilyId { get; init; }
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
        public DateTime ExpiresUtc { get; init; } = DateTime.UtcNow.AddDays(10);
        public DateTime? RevokedUtc { get; set; }

        public bool IsActive => RevokedUtc is null && DateTime.UtcNow < ExpiresUtc;
    }

    private readonly List<Entry> _entries = [];

    public Task<Result<RefreshTokenInfo>> IssueAsync(string userId, CancellationToken cancellationToken = default)
    {
        var entry = Create(userId, Guid.NewGuid());
        _entries.Add(entry);
        return Task.FromResult(Result<RefreshTokenInfo>.Ok(ToInfo(entry)));
    }

    public Task<RefreshTokenInfo?> FindByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var entry = _entries.FirstOrDefault(e => e.Token == token);
        return Task.FromResult(entry is null ? null : ToInfo(entry));
    }

    public Task<Result> RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        var entry = _entries.FirstOrDefault(e => e.Token == token);
        if (entry is null)
        {
            return Task.FromResult(Result.NotFound("refreshToken", "was not found"));
        }

        if (!entry.IsActive)
        {
            return Task.FromResult(Result.Validation("refreshToken", "is not active"));
        }

        entry.RevokedUtc = DateTime.UtcNow;
        return Task.FromResult(Result.Ok());
    }

    public Task<Result<RefreshTokenInfo>> RotateAsync(string token, CancellationToken cancellationToken = default)
    {
        var entry = _entries.FirstOrDefault(e => e.Token == token);
        if (entry is null)
        {
            return Task.FromResult(Result<RefreshTokenInfo>.Unauthorized("refreshToken", "did not match any user"));
        }

        if (!entry.IsActive)
        {
            foreach (var sibling in _entries.Where(e => e.FamilyId == entry.FamilyId && e.IsActive))
            {
                sibling.RevokedUtc = DateTime.UtcNow;
            }

            return Task.FromResult(Result<RefreshTokenInfo>.Unauthorized("refreshToken", "has been revoked"));
        }

        entry.RevokedUtc = DateTime.UtcNow;
        var replacement = Create(entry.UserId, entry.FamilyId);
        _entries.Add(replacement);
        return Task.FromResult(Result<RefreshTokenInfo>.Ok(ToInfo(replacement)));
    }

    public Task<IReadOnlyList<RefreshTokenSummary>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var summaries = _entries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedUtc)
            .Select(e => new RefreshTokenSummary(e.CreatedUtc, e.ExpiresUtc, e.RevokedUtc, e.IsActive))
            .ToList();

        return Task.FromResult<IReadOnlyList<RefreshTokenSummary>>(summaries);
    }

    private static Entry Create(string userId, Guid familyId) =>
        new()
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            UserId = userId,
            FamilyId = familyId
        };

    private static RefreshTokenInfo ToInfo(Entry entry) =>
        new(entry.UserId, entry.Token, entry.ExpiresUtc, entry.CreatedUtc, entry.RevokedUtc);
}
