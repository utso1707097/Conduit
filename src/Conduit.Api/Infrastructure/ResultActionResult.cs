using Conduit.Application.Common;
using Conduit.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Infrastructure;

public static class ResultActionResult
{
    public static IActionResult FromResult<T>(
        this ControllerBase controller,
        Result<T> result,
        Func<T, object> successBody,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return controller.StatusCode(successStatusCode, successBody(result.Value!));
        }

        return result.Kind switch
        {
            ErrorKind.Validation => controller.UnprocessableEntity(
                new ErrorsResponse { Errors = new Dictionary<string, string[]>(result.Errors) }),
            ErrorKind.Conflict => controller.Conflict(
                new ErrorsResponse { Errors = new Dictionary<string, string[]>(result.Errors) }),
            ErrorKind.Unauthorized => controller.Unauthorized(
                new ErrorsResponse { Errors = new Dictionary<string, string[]>(result.Errors) }),
            ErrorKind.NotFound => controller.NotFound(
                new ErrorsResponse { Errors = new Dictionary<string, string[]>(result.Errors) }),
            ErrorKind.Forbidden => controller.StatusCode(
                StatusCodes.Status403Forbidden,
                new ErrorsResponse { Errors = new Dictionary<string, string[]>(result.Errors) }),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
