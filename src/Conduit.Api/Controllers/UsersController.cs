using System.Security.Claims;
using Conduit.Api.Contracts;
using Conduit.Api.Infrastructure;
using Conduit.Api.Routing;
using Conduit.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Controllers;

[ApiController]
[Route(ApiRoutes.Users)]
public sealed class UsersController(
    RegisterUserHandler registerUser,
    LoginUserHandler loginUser,
    RefreshUserTokenHandler refreshUserToken,
    RevokeRefreshTokenHandler revokeRefreshToken,
    ListUserRefreshTokensHandler listRefreshTokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] UserWrapperRequest<RegisterUserRequest>? request,
        CancellationToken cancellationToken)
    {
        var user = request?.User;
        var result = await registerUser.HandleAsync(
            new RegisterUserCommand(
                user?.Username ?? string.Empty,
                user?.Email ?? string.Empty,
                user?.Password ?? string.Empty),
            cancellationToken);

        return this.FromResult(
            result,
            auth => new UserWrapperResponse { User = ToUserResponse(auth) },
            successStatusCode: StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] UserWrapperRequest<LoginUserRequest>? request,
        CancellationToken cancellationToken)
    {
        var user = request?.User;
        var result = await loginUser.HandleAsync(
            new LoginUserCommand(
                user?.Email ?? string.Empty,
                user?.Password ?? string.Empty),
            cancellationToken);

        return this.FromResult(
            result,
            auth => new UserWrapperResponse { User = ToUserResponse(auth) });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await refreshUserToken.HandleAsync(
            new RefreshTokenCommand(request?.RefreshToken ?? string.Empty),
            cancellationToken);

        return this.FromResult(
            result,
            auth => new UserWrapperResponse { User = ToUserResponse(auth) });
    }

    [AllowAnonymous]
    [HttpPost("revoke-refresh")]
    public async Task<IActionResult> RevokeRefresh(
        [FromBody] RevokeRefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await revokeRefreshToken.HandleAsync(
            new RevokeRefreshTokenCommand(request?.RefreshToken ?? string.Empty),
            cancellationToken);

        return this.FromResult(result, () => new MessageResponse { Message = "Token revoked" });
    }

    [Authorize]
    [HttpGet("{id}/refresh-tokens")]
    public async Task<IActionResult> ListRefreshTokens(string id, CancellationToken cancellationToken)
    {
        var requesterId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        var result = await listRefreshTokens.HandleAsync(
            new ListRefreshTokensCommand(id, requesterId),
            cancellationToken);

        return this.FromResult(
            result,
            tokens => tokens.Select(ToRefreshTokenResponse).ToList());
    }

    private static UserResponse ToUserResponse(AuthenticatedUser authenticated) =>
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

    private static RefreshTokenResponse ToRefreshTokenResponse(RefreshTokenInfo token) =>
        new()
        {
            Token = token.Token,
            ExpiresUtc = token.ExpiresUtc,
            CreatedUtc = token.CreatedUtc,
            RevokedUtc = token.RevokedUtc,
            IsActive = token.IsActive
        };
}
