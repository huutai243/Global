namespace ECommerce.Core.SharedLibs.Responses;

public sealed class PagedResponse<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = Array.Empty<T>();

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}
