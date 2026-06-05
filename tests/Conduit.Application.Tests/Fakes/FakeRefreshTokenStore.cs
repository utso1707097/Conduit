using Conduit.Application.Common;
using Conduit.Application.Users;

namespace Conduit.Application.Tests.Fakes;

internal sealed class FakeRefreshTokenStore : IRefreshTokenStore
{
    private readonly Dictionary<string, RefreshTokenInfo> _byToken = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<RefreshTokenInfo>> _byUserId = new(StringComparer.Ordinal);

    public Task<Result<RefreshTokenInfo>> IssueAsync(string userId, CancellationToken cancellationToken = default)
    {
        var active = _byUserId.GetValueOrDefault(userId)?
            .FirstOrDefault(t => t.IsActive);

        if (active is not null)
        {
            return Task.FromResult(Result<RefreshTokenInfo>.Ok(active));
        }

        var token = Create(userId);
        _byToken[token.Token] = token;
        if (!_byUserId.TryGetValue(userId, out var list))
        {
            list = [];
            _byUserId[userId] = list;
        }

        list.Add(token);
        return Task.FromResult(Result<RefreshTokenInfo>.Ok(token));
    }

    public Task<RefreshTokenInfo?> FindByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byToken.GetValueOrDefault(token));

    public Task<Result> RevokeAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!_byToken.TryGetValue(token, out var info))
        {
            return Task.FromResult(Result.NotFound("refreshToken", "was not found"));
        }

        if (!info.IsActive)
        {
            return Task.FromResult(Result.Fail(
                ErrorKind.Validation,
                new Dictionary<string, string[]> { ["refreshToken"] = ["is not active"] }));
        }

        var revoked = info with { RevokedUtc = DateTime.UtcNow };
        _byToken[token] = revoked;
        ReplaceInUserList(revoked);
        return Task.FromResult(Result.Ok());
    }

    public Task<Result<RefreshTokenInfo>> RotateAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!_byToken.TryGetValue(token, out var info))
        {
            return Task.FromResult(Result<RefreshTokenInfo>.Fail(
                ErrorKind.Unauthorized,
                new Dictionary<string, string[]> { ["refreshToken"] = ["did not match any user"] }));
        }

        if (!info.IsActive)
        {
            return Task.FromResult(Result<RefreshTokenInfo>.Fail(
                ErrorKind.Unauthorized,
                new Dictionary<string, string[]> { ["refreshToken"] = ["is not active"] }));
        }

        var revoked = info with { RevokedUtc = DateTime.UtcNow };
        _byToken[token] = revoked;
        ReplaceInUserList(revoked);

        var replacement = Create(info.UserId);
        _byToken[replacement.Token] = replacement;
        _byUserId[info.UserId].Add(replacement);
        return Task.FromResult(Result<RefreshTokenInfo>.Ok(replacement));
    }

    public Task<IReadOnlyList<RefreshTokenInfo>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RefreshTokenInfo>>(
            _byUserId.GetValueOrDefault(userId)?.ToList() ?? []);

    private static RefreshTokenInfo Create(string userId)
    {
        var now = DateTime.UtcNow;
        return new RefreshTokenInfo(
            userId,
            Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            now.AddDays(10),
            now,
            null);
    }

    private void ReplaceInUserList(RefreshTokenInfo updated)
    {
        if (!_byUserId.TryGetValue(updated.UserId, out var list))
        {
            return;
        }

        var index = list.FindIndex(t => t.Token == updated.Token);
        if (index >= 0)
        {
            list[index] = updated;
        }
    }
}
