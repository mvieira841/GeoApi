using System.Text.Json.Serialization;

namespace GeoApi.Abstractions.Requests.Cities;

/// <summary>
/// Represents the query parameters for retrieving a paged list of cities.
/// Inherits from PagedRequest for sorting and pagination.
/// </summary>
public record GetAllCitiesRequest : PagedRequest
{
    /// <summary>
    /// Optional. Filter cities by name (case-insensitive, partial match).
    /// </summary>
    public string? Name { get; set; } = null;

    /// <summary>
    /// Optional. Filter cities by an exact latitude.
    /// </summary>
    public decimal? Latitude { get; set; } = null;

    /// <summary>
    /// Optional. Filter cities by an exact longitude.
    /// </summary>
    public decimal? Longitude { get; set; } = null;

    /// <summary>
    /// Overridden internal property to get the validated sort column.
    /// Defaults to "Name".
    /// </summary>
    [JsonIgnore]
    public override string SortColumnValue => SortColumn ?? "Name";
}