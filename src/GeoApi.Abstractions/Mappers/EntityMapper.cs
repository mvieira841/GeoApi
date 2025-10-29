using GeoApi.Abstractions.Responses.Cities;
using GeoApi.Abstractions.Responses.Countries;

namespace GeoApi.Abstractions.Mappers;

public static class EntityMapper
{
    public static CountryResponse ToResponse(this Entities.Country country)
    {
        ArgumentNullException.ThrowIfNull(country);

        return new CountryResponse(
            country.Id,
            country.Name,
            country.IsoCode);
    }

    public static CityResponse ToResponse(this Entities.City city)
    {
        ArgumentNullException.ThrowIfNull(city);

        return new CityResponse(
            city.Id,
            city.Name,
            city.Latitude,
            city.Longitude,
            city.CountryId
        );
    }
}
