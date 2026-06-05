using Conduit.Application.Common;

namespace Conduit.Application.Users;

public sealed record UserAccount(
    string Id,
    string UserName,
    string Email,
    string? Bio,
    string? Image);

public sealed record AuthenticatedUser(UserAccount Account, string Token);

public sealed record RegisterUserCommand(string UserName, string Email, string Password);

public sealed record LoginUserCommand(string Email, string Password);

public interface IUserAccountStore
{
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);

    Task<Result<UserAccount>> CreateAsync(
        string userName,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<UserAccount?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}

public interface ITokenIssuer
{
    string IssueToken(UserAccount user);
}
