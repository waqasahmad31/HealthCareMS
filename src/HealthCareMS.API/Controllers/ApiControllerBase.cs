using HealthCareMS.Shared.Api;
using HealthCareMS.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult FromResult<T>(Result<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatusCode, ApiResponse<T>.Ok(result.Value, HttpContext.TraceIdentifier));
        }

        return StatusCode(ToStatusCode(result.Error.Code), ApiResponse<T>.Fail(result.Error, HttpContext.TraceIdentifier));
    }

    protected IActionResult FromResult(Result result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return StatusCode(successStatusCode, ApiResponse<EmptyResponse>.Ok(new EmptyResponse(), HttpContext.TraceIdentifier));
        }

        return StatusCode(ToStatusCode(result.Error.Code), ApiResponse<EmptyResponse>.Fail(result.Error, HttpContext.TraceIdentifier));
    }

    protected IActionResult OkEnvelope<T>(T data)
    {
        return Ok(ApiResponse<T>.Ok(data, HttpContext.TraceIdentifier));
    }

    protected IActionResult Fail(Error error)
    {
        return StatusCode(ToStatusCode(error.Code), ApiResponse<EmptyResponse>.Fail(error, HttpContext.TraceIdentifier));
    }

    private static int ToStatusCode(string errorCode)
    {
        return errorCode switch
        {
            "VALIDATION_ERROR" => StatusCodes.Status400BadRequest,
            "AUTH_INVALID_CREDENTIALS" => StatusCodes.Status401Unauthorized,
            "AUTH_ACCOUNT_LOCKED" => StatusCodes.Status423Locked,
            "AUTH_ACCOUNT_INACTIVE" => StatusCodes.Status403Forbidden,
            "AUTH_EMAIL_NOT_VERIFIED" => StatusCodes.Status403Forbidden,
            _ when errorCode.EndsWith("_FORBIDDEN", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status403Forbidden,
            _ when errorCode.EndsWith("_NOT_FOUND", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status404NotFound,
            _ when errorCode.EndsWith("_EXISTS", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
            _ when errorCode.EndsWith("_INVALID", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };
    }
}
