using System.Security.Claims;
using Conduit.Api.Infrastructure;
using Conduit.Api.Routing;
using Conduit.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Users;

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
        [FromBody] UserWrapperRequest<RegisterUserCommand>? request,
        CancellationToken cancellationToken)
    {
        var result = await registerUser.HandleAsync(
            request?.User ?? new RegisterUserCommand(),
            cancellationToken);

        return this.FromResult(
            result,
            auth => new UserWrapperResponse { User = UserResponse.From(auth) },
            successStatusCode: StatusCodes.Status201Created);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] UserWrapperRequest<LoginUserCommand>? request,
        CancellationToken cancellationToken)
    {
        var result = await loginUser.HandleAsync(
            request?.User ?? new LoginUserCommand(),
            cancellationToken);

        return this.FromResult(
            result,
            auth => new UserWrapperResponse { User = UserResponse.From(auth) });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand? request,
        CancellationToken cancellationToken)
    {
        var result = await refreshUserToken.HandleAsync(
            request ?? new RefreshTokenCommand(),
            cancellationToken);

        return this.FromResult(
            result,
            auth => new UserWrapperResponse { User = UserResponse.From(auth) });
    }

    [AllowAnonymous]
    [HttpPost("revoke-refresh")]
    public async Task<IActionResult> RevokeRefresh(
        [FromBody] RevokeRefreshTokenCommand? request,
        CancellationToken cancellationToken)
    {
        var result = await revokeRefreshToken.HandleAsync(
            request ?? new RevokeRefreshTokenCommand(),
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
            tokens => tokens.Select(RefreshTokenResponse.From).ToList());
    }
}
