namespace GeoApi.Abstractions.Requests.Cities;

/// <summary>
/// Request model for creating a new city.
/// </summary>
/// <param name="Name">The name of the city.</param>
/// <param name="Latitude">The latitude coordinate of the city.</param>
/// <param name="Longitude">The longitude coordinate of the city.</param>
public record CreateCityRequest(string Name, decimal Latitude, decimal Longitude);