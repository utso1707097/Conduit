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
}

public sealed class UserWrapperResponse
{
    [Required]
    public required UserResponse User { get; init; }
}

public sealed class ErrorsResponse
{
    [Required]
    public required Dictionary<string, string[]> Errors { get; init; }
}
