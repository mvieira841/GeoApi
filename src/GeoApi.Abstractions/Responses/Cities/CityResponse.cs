using GeoApi.Abstractions.Entities;

namespace GeoApi.Abstractions.Responses.Cities;

/// <summary>
/// Represents a city in a response.
/// </summary>
/// <param name="Id">The unique identifier for the city.</param>
/// <param name="Name">The name of the city.</param>
/// <param name="Latitude">The latitude coordinate of the city.</param>
/// <param name="Longitude">The longitude coordinate of the city.</param>
/// <param name="CountryId">The unique identifier of the parent country.</param>
public record CityResponse(Guid Id, string Name, decimal Latitude, decimal Longitude, Guid CountryId);