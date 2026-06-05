using Conduit.Application.Common;

namespace Conduit.Application.Users;

public sealed class RefreshUserTokenHandler(
    IUserAccountStore users,
    ITokenIssuer tokens,
    IRefreshTokenStore refreshTokens)
{
    public async Task<Result<AuthenticatedUser>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result<AuthenticatedUser>.Validation("refreshToken", "can't be blank");
        }

        var rotateResult = await refreshTokens.RotateAsync(command.Token.Trim(), cancellationToken);
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
