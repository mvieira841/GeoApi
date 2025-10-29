using FluentResults;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Cities;
using GeoApi.Abstractions.Responses.Cities;

namespace GeoApi.Abstractions.Interfaces.Managers;

public interface ICityManager
{
    Task<Result<CityResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedList<CityResponse>>> GetAllForCountryAsync(Guid countryId, GetAllCitiesRequest request, CancellationToken ct = default);
    Task<Result<CityResponse>> CreateAsync(Guid countryId, CreateCityRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid id, UpdateCityRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}