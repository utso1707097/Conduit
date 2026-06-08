using Conduit.Application.Common;

namespace Conduit.Application.Users;

public sealed record UserAccount(
    string Id,
    string UserName,
    string Email,
    string? Bio,
    string? Image);

// Token carries the plaintext refresh-token value. It is only populated when a
// token is freshly issued or rotated (the single moment the secret is known);
// the value is never persisted in clear text. Use RefreshTokenSummary for listings.
public sealed record RefreshTokenInfo(
    string UserId,
    string Token,
    DateTime ExpiresUtc,
    DateTime CreatedUtc,
    DateTime? RevokedUtc)
{
    public bool IsActive => RevokedUtc is null && DateTime.UtcNow < ExpiresUtc;
}

// Leak-safe projection for listing endpoints: deliberately omits the secret value.
public sealed record RefreshTokenSummary(
    DateTime CreatedUtc,
    DateTime ExpiresUtc,
    DateTime? RevokedUtc,
    bool IsActive);

public sealed record AuthenticatedUser(
    UserAccount Account,
    string Token,
    RefreshTokenInfo RefreshToken);

public sealed record UserWithToken(UserAccount Account, string Token);

public sealed record UpdateUserChanges(
    string? Email = null,
    string? UserName = null,
    string? Password = null,
    string? Bio = null,
    string? Image = null);

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

    Task<Result<UserAccount>> UpdateAsync(
        string userId,
        UpdateUserChanges changes,
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

    Task<IReadOnlyList<RefreshTokenSummary>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
