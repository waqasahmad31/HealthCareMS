namespace HealthCareMS.Shared.Common;

public sealed record PaginationRequest(
    int Page = 1,
    int PageSize = 10,
    string SortBy = "CreatedAt",
    string SortOrder = "Desc",
    string? Search = null)
{
    public int SafePage => Page < 1 ? 1 : Page;

    public int SafePageSize => PageSize switch
    {
        < 1 => 10,
        > 100 => 100,
        _ => PageSize
    };
}

public sealed record PaginationMeta(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasNext,
    bool HasPrev);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    PaginationMeta Pagination);
