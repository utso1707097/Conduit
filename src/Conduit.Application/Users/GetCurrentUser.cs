using Conduit.Application.Common;

namespace Conduit.Application.Users;

public sealed record GetCurrentUserQuery(string UserId);

public sealed class GetCurrentUserHandler(IUserAccountStore users)
{
    public async Task<Result<UserAccount>> HandleAsync(
        GetCurrentUserQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return Result<UserAccount>.Fail(
                ErrorKind.Unauthorized,
                new Dictionary<string, string[]>
                {
                    ["user"] = ["is not authenticated"]
                });
        }

        var account = await users.FindByIdAsync(query.UserId, cancellationToken);
        if (account is null)
        {
            return Result<UserAccount>.NotFound("user", "was not found");
        }

        return Result<UserAccount>.Ok(account);
    }
}
