namespace GeoApi.Abstractions.Requests.Countries;

/// <summary>
/// Request model for creating a new country.
/// </summary>
/// <param name="Name">The name of the country.</param>
/// <param name="IsoCode">The 3-letter ISO code for the country.</param>
public record CreateCountryRequest(string Name, string IsoCode);