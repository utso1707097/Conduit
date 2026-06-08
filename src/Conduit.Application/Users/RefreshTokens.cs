using System.Text.Json.Serialization;
using Conduit.Application.Common;

namespace Conduit.Application.Users;

public sealed record RefreshTokenCommand
{
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed record RevokeRefreshTokenCommand
{
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; init; } = string.Empty;
}

public sealed record ListRefreshTokensCommand(string UserId, string RequesterUserId);

public sealed class RefreshUserTokenHandler(
    IUserAccountStore users,
    ITokenIssuer tokens,
    IRefreshTokenStore refreshTokens)
{
    public async Task<Result<AuthenticatedUser>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result<AuthenticatedUser>.Validation("refreshToken", "can't be blank");
        }

        var rotateResult = await refreshTokens.RotateAsync(command.RefreshToken.Trim(), cancellationToken);
        if (!rotateResult.IsSuccess)
        {
            return Result<AuthenticatedUser>.Fail(rotateResult.Kind, rotateResult.Errors);
        }

        var refreshToken = rotateResult.Value!;
        var account = await users.FindByIdAsync(refreshToken.UserId, cancellationToken);
        if (account is null)
        {
            return Result<AuthenticatedUser>.Fail(
                ErrorKind.Unauthorized,
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = ["did not match any user"]
                });
        }

        var jwt = tokens.IssueToken(account);
        return Result<AuthenticatedUser>.Ok(new AuthenticatedUser(account, jwt, refreshToken));
    }
}

public sealed class RevokeRefreshTokenHandler(IRefreshTokenStore refreshTokens)
{
    public async Task<Result> HandleAsync(
        RevokeRefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Result.Fail(
                ErrorKind.Validation,
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = ["can't be blank"]
                });
        }

        return await refreshTokens.RevokeAsync(command.RefreshToken.Trim(), cancellationToken);
    }
}

public sealed class ListUserRefreshTokensHandler(IRefreshTokenStore refreshTokens)
{
    public async Task<Result<IReadOnlyList<RefreshTokenSummary>>> HandleAsync(
        ListRefreshTokensCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return Result<IReadOnlyList<RefreshTokenSummary>>.Validation("userId", "can't be blank");
        }

        if (!string.Equals(command.UserId, command.RequesterUserId, StringComparison.Ordinal))
        {
            return Result<IReadOnlyList<RefreshTokenSummary>>.Fail(
                ErrorKind.Forbidden,
                new Dictionary<string, string[]>
                {
                    ["userId"] = ["does not match authenticated user"]
                });
        }

        var tokens = await refreshTokens.ListForUserAsync(command.UserId, cancellationToken);
        return Result<IReadOnlyList<RefreshTokenSummary>>.Ok(tokens);
    }
}
