using HealthCareMS.Shared.Common;

namespace HealthCareMS.Shared.Api;

public sealed record ApiMeta(string RequestId, DateTimeOffset Timestamp, PaginationMeta? Pagination = null);

public sealed record ApiError(string Code, string Message, IReadOnlyList<ValidationError>? Details = null)
{
    public static ApiError From(Error error)
    {
        return new(error.Code, error.Message, error.Details);
    }
}

public sealed record ApiResponse<T>(bool Success, T? Data, ApiError? Error, ApiMeta Meta)
{
    public static ApiResponse<T> Ok(T data, string requestId, PaginationMeta? pagination = null)
    {
        return new(true, data, null, new ApiMeta(requestId, DateTimeOffset.UtcNow, pagination));
    }

    public static ApiResponse<T> Fail(Error error, string requestId)
    {
        return new(false, default, ApiError.From(error), new ApiMeta(requestId, DateTimeOffset.UtcNow));
    }
}

public sealed record EmptyResponse;
