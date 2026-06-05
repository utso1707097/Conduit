using Conduit.Application.Common;
using Conduit.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Api.Infrastructure;

public static class ResultActionResult
{
    public static IActionResult FromResult(
        this ControllerBase controller,
        Result result,
        Func<object> successBody,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return controller.StatusCode(successStatusCode, successBody());
        }

        return MapFailure(controller, result.Kind, result.Errors);
    }

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

        return MapFailure(controller, result.Kind, result.Errors);
    }

    private static IActionResult MapFailure(
        ControllerBase controller,
        ErrorKind kind,
        IReadOnlyDictionary<string, string[]> errors) =>
        kind switch
        {
            ErrorKind.Validation => controller.UnprocessableEntity(
                new ErrorsResponse { Errors = new Dictionary<string, string[]>(errors) }),
            ErrorKind.Conflict => controller.Conflict(
                new ErrorsResponse { Errors = new Dictionary<string, string[]>(errors) }),
            ErrorKind.Unauthorized => controller.Unauthorized(
                new ErrorsResponse { Errors = new Dictionary<string, string[]>(errors) }),
            ErrorKind.NotFound => controller.NotFound(
                new ErrorsResponse { Errors = new Dictionary<string, string[]>(errors) }),
            ErrorKind.Forbidden => controller.StatusCode(
                StatusCodes.Status403Forbidden,
                new ErrorsResponse { Errors = new Dictionary<string, string[]>(errors) }),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError)
        };
}
