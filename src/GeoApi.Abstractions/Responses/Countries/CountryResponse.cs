namespace GeoApi.Abstractions.Responses.Countries;

/// <summary>
/// Represents a country in a response.
/// </summary>
/// <param name="Id">The unique identifier for the country.</param>
/// <param name="Name">The name of the country.</param>
/// <param name="IsoCode">The 3-letter ISO code for the country.</param>
public record CountryResponse(
    Guid Id,
    string Name,
    string IsoCode);