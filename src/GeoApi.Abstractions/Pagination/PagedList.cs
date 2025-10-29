namespace GeoApi.Abstractions.Pagination;

/// <summary>
/// Represents a paginated list of items.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public class PagedList<T>
{
    /// <summary>
    /// The list of items for the current page.
    /// </summary>
    public List<T> Items { get; }

    /// <summary>
    /// The current page number.
    /// </summary>
    /// <example>1</example>
    public int Page { get; } = 1;

    /// <summary>
    /// The number of items per page.
    /// </summary>
    /// <example>10</example>
    public int PageSize { get; } = 10;

    /// <summary>
    /// The total number of items available in the full list.
    /// </summary>
    /// <example>100</example>
    public int TotalCount { get; }

    /// <summary>
    /// Indicates if there is a next page of items.
    /// </summary>
    /// <example>true</example>
    public bool HasNextPage => Page * PageSize < TotalCount;

    /// <summary>
    /// Indicates if there is a previous page of items.
    /// </summary>
    /// <example>false</example>
    public bool HasPreviousPage => Page > 1;

    public PagedList(List<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}