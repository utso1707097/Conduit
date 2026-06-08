using System.ComponentModel.DataAnnotations;
using Conduit.Application.Users;

namespace Conduit.Api.Users;

public sealed class UserWrapperRequest<T>
{
    public T? User { get; set; }
}

public sealed class UserResponse
{
    [Required]
    public required string Email { get; init; }

    [Required]
    public required string Token { get; init; }

    [Required]
    public required string Username { get; init; }

    public string? Bio { get; init; }

    public string? Image { get; init; }

    public string? RefreshToken { get; init; }

    public DateTime? RefreshTokenExpiration { get; init; }

    public static UserResponse From(AuthenticatedUser authenticated) =>
        new()
        {
            Email = authenticated.Account.Email,
            Token = authenticated.Token,
            Username = authenticated.Account.UserName,
            Bio = authenticated.Account.Bio,
            Image = authenticated.Account.Image,
            RefreshToken = authenticated.RefreshToken.Token,
            RefreshTokenExpiration = authenticated.RefreshToken.ExpiresUtc
        };

    public static UserResponse From(UserAccount account, string token) =>
        new()
        {
            Email = account.Email,
            Token = token,
            Username = account.UserName,
            Bio = account.Bio,
            Image = account.Image
        };

    public static UserResponse From(UserWithToken user) =>
        From(user.Account, user.Token);
}

public sealed class UserWrapperResponse
{
    [Required]
    public required UserResponse User { get; init; }
}

public sealed class RefreshTokenResponse
{
    [Required]
    public required DateTime ExpiresUtc { get; init; }

    [Required]
    public required DateTime CreatedUtc { get; init; }

    public DateTime? RevokedUtc { get; init; }

    [Required]
    public required bool IsActive { get; init; }

    // Intentionally omits the token value: refresh tokens are write-once secrets and
    // must never be echoed back through a listing endpoint.
    public static RefreshTokenResponse From(RefreshTokenSummary token) =>
        new()
        {
            ExpiresUtc = token.ExpiresUtc,
            CreatedUtc = token.CreatedUtc,
            RevokedUtc = token.RevokedUtc,
            IsActive = token.IsActive
        };
}

public sealed class MessageResponse
{
    [Required]
    public required string Message { get; init; }
}

public sealed class ErrorsResponse
{
    [Required]
    public required Dictionary<string, string[]> Errors { get; init; }
}
