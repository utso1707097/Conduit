using Conduit.Application.Common;

namespace Conduit.Application.Users;

public sealed class RevokeRefreshTokenHandler(IRefreshTokenStore refreshTokens)
{
    public async Task<Result> HandleAsync(
        RevokeRefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result.Fail(
                ErrorKind.Validation,
                new Dictionary<string, string[]>
                {
                    ["refreshToken"] = ["can't be blank"]
                });
        }

        return await refreshTokens.RevokeAsync(command.Token.Trim(), cancellationToken);
    }
}
