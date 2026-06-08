using Conduit.Api.Infrastructure;
using Conduit.Api.Routing;
using Conduit.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Users;

[ApiController]
[Authorize]
[Route(ApiRoutes.User)]
public sealed class UserController(
    GetCurrentUserHandler getCurrentUser,
    UpdateCurrentUserHandler updateCurrentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await getCurrentUser.HandleAsync(
            new GetCurrentUserQuery(userId),
            cancellationToken);

        return this.FromResult(
            result,
            account => new UserWrapperResponse
            {
                User = UserResponse.From(account, GetBearerToken())
            });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateCurrent(
        [FromBody] UserWrapperRequest<UpdateCurrentUserCommand>? request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await updateCurrentUser.HandleAsync(
            new UpdateCurrentUserRequest(userId, request?.User ?? new UpdateCurrentUserCommand()),
            cancellationToken);

        return this.FromResult(
            result,
            user => new UserWrapperResponse { User = UserResponse.From(user) });
    }

    private string GetBearerToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Token ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization["Token ".Length..].Trim();
        }

        return string.Empty;
    }
}
