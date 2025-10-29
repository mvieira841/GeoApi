using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Cities;

namespace GeoApi.Abstractions.Interfaces.Access;

public interface ICityRepository
{
    Task<City?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedList<City>> GetAllForCountryAsync(Guid countryId, GetAllCitiesRequest request, CancellationToken ct = default);
    Task<Guid> CreateAsync(City city, CancellationToken ct = default);
    void Update(City city);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}