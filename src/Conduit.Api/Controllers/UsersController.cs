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
    LoginUserHandler loginUser) : ControllerBase
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

    private static UserResponse ToUserResponse(AuthenticatedUser authenticated) =>
        new()
        {
            Email = authenticated.Account.Email,
            Token = authenticated.Token,
            Username = authenticated.Account.UserName,
            Bio = authenticated.Account.Bio,
            Image = authenticated.Account.Image
        };
}
