using FluentResults;
using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Interfaces.Managers;
using GeoApi.Abstractions.Mappers;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Cities;
using GeoApi.Abstractions.Responses.Cities;
using GeoApi.Manager.Utility;

namespace GeoApi.Manager.Managers;

public sealed class CityManager(
    ICityRepository cityRepository,
    ICountryRepository countryRepository,
    IUnitOfWork unitOfWork)
    : ICityManager
{
    public async Task<Result<CityResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var city = await cityRepository.GetByIdAsync(id, ct);
        return city is null
            ? Result.Fail(DomainErrors.CityNotFound)
            : city.ToResponse();
    }

    public async Task<Result<PagedList<CityResponse>>> GetAllForCountryAsync(Guid countryId, GetAllCitiesRequest request, CancellationToken ct = default)
    {
        if (!await countryRepository.ExistsAsync(countryId, ct))
            return Result.Fail(DomainErrors.CountryNotFound);

        var pagedList = await cityRepository.GetAllForCountryAsync(countryId, request, ct);
        var response = pagedList.ToResponse(city => city.ToResponse());
        return Result.Ok(response);
    }

    public async Task<Result<CityResponse>> CreateAsync(Guid countryId, CreateCityRequest request, CancellationToken ct = default)
    {
        if (!await countryRepository.ExistsAsync(countryId, ct))
            return Result.Fail(DomainErrors.CountryNotFound);

        var city = new City
        {
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CountryId = countryId
        };

        await cityRepository.CreateAsync(city, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return city.ToResponse();
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateCityRequest request, CancellationToken ct = default)
    {
        var city = await cityRepository.GetByIdAsync(id, ct);
        if (city is null)
            return Result.Fail(DomainErrors.CityNotFound);

        city.Name = request.Name;
        city.Latitude = request.Latitude;
        city.Longitude = request.Longitude;

        cityRepository.Update(city);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await cityRepository.DeleteAsync(id, ct);
        if (!deleted)
            return Result.Fail(DomainErrors.CityNotFound);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }
}