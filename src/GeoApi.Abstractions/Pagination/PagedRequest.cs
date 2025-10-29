using System.Text.Json.Serialization;

/// <summary>
/// Represents common query parameters for pagination and sorting.
/// </summary>
public record PagedRequest
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    /// <summary>
    /// The page number to retrieve.
    /// </summary>
    /// <example>1</example>
    public int? Page { get; set; }

    /// <summary>
    /// The number of items to retrieve per page.
    /// </summary>
    /// <example>10</example>
    public int? PageSize { get; set; }

    /// <summary>
    /// The name of the property to sort by. Case-insensitive.
    /// This property is bound from the API request.
    /// </summary>
    /// <example>Name</example>
    public string? SortColumn { get; set; }

    /// <summary>
    /// The sort order.
    /// </summary>
    /// <example>ASC</example>
    public string? SortOrder { get; set; }

    /// <summary>
    /// Internal property to get the validated sort column. Defaults to "Id".
    /// Your managers/repositories should use this property.
    /// </summary>
    [JsonIgnore]
    public virtual string SortColumnValue => SortColumn ?? "Id";

    /// <summary>
    /// Internal property to get the validated order column. Defaults to "ASC".
    /// Your managers/repositories should use this property.
    /// </summary>
    [JsonIgnore]
    public string SortOrderValue => SortOrder ?? "ASC";

    /// <summary>
    /// Internal property to get the validated page number. Defaults to 1.
    /// </summary>
    [JsonIgnore]
    public int PageNumber => Page ?? DefaultPage;

    /// <summary>
    /// Internal property to get the validated and capped page size. Defaults to 10, max is 100.
    /// </summary>
    [JsonIgnore]
    public int PageSizeValue
    {
        get
        {
            int size = PageSize ?? DefaultPageSize;
            return (size > MaxPageSize) ? MaxPageSize : size;
        }
    }
}