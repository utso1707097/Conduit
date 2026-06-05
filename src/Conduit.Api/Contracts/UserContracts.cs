using System.ComponentModel.DataAnnotations;

namespace Conduit.Api.Contracts;

public sealed class UserWrapperRequest<T>
{
    public T? User { get; set; }
}

public sealed class RegisterUserRequest
{
    public string? Username { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }
}

public sealed class LoginUserRequest
{
    public string? Email { get; set; }

    public string? Password { get; set; }
}

public sealed class RefreshTokenRequest
{
    public string? RefreshToken { get; set; }
}

public sealed class RevokeRefreshTokenRequest
{
    public string? RefreshToken { get; set; }
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
}

public sealed class UserWrapperResponse
{
    [Required]
    public required UserResponse User { get; init; }
}

public sealed class RefreshTokenResponse
{
    [Required]
    public required string Token { get; init; }

    [Required]
    public required DateTime ExpiresUtc { get; init; }

    [Required]
    public required DateTime CreatedUtc { get; init; }

    public DateTime? RevokedUtc { get; init; }

    [Required]
    public required bool IsActive { get; init; }
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
