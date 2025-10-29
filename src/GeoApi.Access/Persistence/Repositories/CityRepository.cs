using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Cities;
using GeoApi.Access.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GeoApi.Access.Persistence.Repositories;

internal sealed class CityRepository(ApplicationDbContext dbContext)
    : RepositoryBase<City>(dbContext), ICityRepository
{
    public async Task<City?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await DbContext.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<PagedList<City>> GetAllForCountryAsync(Guid countryId, GetAllCitiesRequest request, CancellationToken ct = default)
    {
        var query = DbContext.Cities
            .Where(c => c.CountryId == countryId)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.Name))
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{request.Name}%"));

        if(request.Latitude is not null)
            query = query.Where(c => c.Latitude == request.Latitude);

        if(request.Longitude is not null)
            query = query.Where(c => c.Longitude == request.Longitude);

        return await CreatePagedListAsync(query, request, ct);
    }

    public async Task<Guid> CreateAsync(City city, CancellationToken ct = default)
    {
        await DbContext.Cities.AddAsync(city, ct);
        return city.Id;
    }

    public void Update(City city)
    {
        DbContext.Cities.Update(city);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var city = await DbContext.Cities.FindAsync([id], ct);
        if (city is null)
            return false;

        DbContext.Cities.Remove(city);
        return true;
    }
}