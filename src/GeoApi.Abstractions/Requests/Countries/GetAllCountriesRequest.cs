using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Text.Json.Serialization;

namespace GeoApi.Abstractions.Requests.Countries;

/// <summary>
/// Represents the query parameters for retrieving a paged list of countries.
/// Inherits from PagedRequest for sorting and pagination.
/// </summary>
public record GetAllCountriesRequest : PagedRequest
{
    /// <summary>
    /// Optional. Filter countries by name (case-insensitive, partial match).
    /// </summary>
    public string? Name { get; set; } = null;

    /// <summary>
    /// Optional. Filter countries by the exact 3-letter ISO code.
    /// </summary>
    public string? IsoCode { get; set; } = null;

    /// <summary>
    /// Overridden internal property to get the validated sort column.
    /// Defaults to "Name".
    /// </summary>
    [JsonIgnore]
    public override string SortColumnValue => SortColumn ?? "Name";
}