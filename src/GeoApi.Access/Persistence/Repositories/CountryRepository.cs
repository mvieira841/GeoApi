using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Countries;
using GeoApi.Access.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GeoApi.Access.Persistence.Repositories;

internal sealed class CountryRepository(ApplicationDbContext dbContext)
    : RepositoryBase<Country>(dbContext), ICountryRepository
{
    public async Task<Country?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await DbContext.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Country?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await DbContext.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, ct);
    }

    public async Task<PagedList<Country>> GetAllAsync(GetAllCountriesRequest request, CancellationToken ct = default)
    {
        var query = DbContext.Countries
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.Name))
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{request.Name}%"));

        if (!string.IsNullOrEmpty(request.IsoCode))
            query = query.Where(c => EF.Functions.Like(c.IsoCode, $"%{request.IsoCode}%"));

        return await CreatePagedListAsync(query, request, ct);
    }

    public async Task<Guid> CreateAsync(Country country, CancellationToken ct = default)
    {
        await DbContext.Countries.AddAsync(country, ct);
        return country.Id;
    }

    public void Update(Country country)
    {
        DbContext.Countries.Update(country);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var country = await DbContext.Countries.FindAsync([id], ct);
        if (country is null)
            return false;

        DbContext.Countries.Remove(country);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        return await DbContext.Countries.AnyAsync(c => c.Id == id, ct);
    }
}