namespace GeoApi.Abstractions.Requests.Countries;

/// <summary>
/// Request model for updating an existing country.
/// </summary>
/// <param name="Name">The new name of the country.</param>
/// <param name="IsoCode">The new 3-letter ISO code for the country.</param>
public record UpdateCountryRequest(string Name, string IsoCode);