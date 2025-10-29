using FluentResults;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Countries;
using GeoApi.Abstractions.Responses.Countries;

namespace GeoApi.Abstractions.Interfaces.Managers;

public interface ICountryManager
{
    Task<Result<CountryResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<PagedList<CountryResponse>>> GetAllAsync(GetAllCountriesRequest request, CancellationToken ct = default);
    Task<Result<CountryResponse>> CreateAsync(CreateCountryRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(Guid id, UpdateCountryRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}