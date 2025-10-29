namespace GeoApi.Abstractions.Requests.Cities;

/// <summary>
/// Request model for updating an existing city.
/// </summary>
/// <param name="Name">The new name of the city.</param>
/// <param name="Latitude">The new latitude coordinate of the city.</param>
/// <param name="Longitude">The new longitude coordinate of the city.</param>
public record UpdateCityRequest(string Name, decimal Latitude, decimal Longitude);