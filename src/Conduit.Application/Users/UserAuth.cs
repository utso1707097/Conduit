using Conduit.Application.Common;

namespace Conduit.Application.Users;

public sealed record UserAccount(
    string Id,
    string UserName,
    string Email,
    string? Bio,
    string? Image);

public sealed record RefreshTokenInfo(
    string UserId,
    string Token,
    DateTime ExpiresUtc,
    DateTime CreatedUtc,
    DateTime? RevokedUtc)
{
    public bool IsActive => RevokedUtc is null && DateTime.UtcNow < ExpiresUtc;
}

public sealed record AuthenticatedUser(
    UserAccount Account,
    string Token,
    RefreshTokenInfo RefreshToken);

public sealed record RegisterUserCommand(string UserName, string Email, string Password);

public sealed record LoginUserCommand(string Email, string Password);

public sealed record RefreshTokenCommand(string Token);

public sealed record RevokeRefreshTokenCommand(string Token);

public sealed record ListRefreshTokensCommand(string UserId, string RequesterUserId);

public interface IUserAccountStore
{
    Task<UserAccount?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

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

public interface IRefreshTokenStore
{
    Task<Result<RefreshTokenInfo>> IssueAsync(string userId, CancellationToken cancellationToken = default);

    Task<RefreshTokenInfo?> FindByTokenAsync(string token, CancellationToken cancellationToken = default);

    Task<Result> RevokeAsync(string token, CancellationToken cancellationToken = default);

    Task<Result<RefreshTokenInfo>> RotateAsync(string token, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshTokenInfo>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
