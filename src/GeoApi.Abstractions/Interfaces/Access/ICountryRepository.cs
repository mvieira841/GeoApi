using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Countries;

namespace GeoApi.Abstractions.Interfaces.Access;

public interface ICountryRepository
{
    Task<Country?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Country?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<PagedList<Country>> GetAllAsync(GetAllCountriesRequest request, CancellationToken ct = default);
    Task<Guid> CreateAsync(Country country, CancellationToken ct = default);
    void Update(Country country);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}