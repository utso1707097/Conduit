using Conduit.Application.Common;

namespace Conduit.Application.Users;

public sealed class ListUserRefreshTokensHandler(IRefreshTokenStore refreshTokens)
{
    public async Task<Result<IReadOnlyList<RefreshTokenInfo>>> HandleAsync(
        ListRefreshTokensCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return Result<IReadOnlyList<RefreshTokenInfo>>.Validation("userId", "can't be blank");
        }

        if (!string.Equals(command.UserId, command.RequesterUserId, StringComparison.Ordinal))
        {
            return Result<IReadOnlyList<RefreshTokenInfo>>.Fail(
                ErrorKind.Forbidden,
                new Dictionary<string, string[]>
                {
                    ["userId"] = ["does not match authenticated user"]
                });
        }

        var tokens = await refreshTokens.ListForUserAsync(command.UserId, cancellationToken);
        return Result<IReadOnlyList<RefreshTokenInfo>>.Ok(tokens);
    }
}
