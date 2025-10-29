using FluentResults;
using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Interfaces.Managers;
using GeoApi.Abstractions.Mappers;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Countries;
using GeoApi.Abstractions.Responses.Countries;
using GeoApi.Manager.Utility;

namespace GeoApi.Manager.Managers;

public sealed class CountryManager(
    ICountryRepository countryRepository,
    IUnitOfWork unitOfWork)
    : ICountryManager
{
    public async Task<Result<CountryResponse>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var country = await countryRepository.GetByIdAsync(id, ct);

        return country is null
                ? Result.Fail(DomainErrors.CountryNotFound)
                : country.ToResponse();
    }

    public async Task<Result<PagedList<CountryResponse>>> GetAllAsync(GetAllCountriesRequest request, CancellationToken ct = default)
    {
        var pagedList = await countryRepository.GetAllAsync(request, ct);
        var response = pagedList.ToResponse(country => country.ToResponse());
        return Result.Ok(response);
    }

    public async Task<Result<CountryResponse>> CreateAsync(CreateCountryRequest request, CancellationToken ct = default)
    {
        var existing = await countryRepository.GetByNameAsync(request.Name, ct);
        if (existing is not null)
            return Result.Fail(DomainErrors.CountryConflict);

        var country = new Country
        {
            Name = request.Name,
            IsoCode = request.IsoCode
        };

        await countryRepository.CreateAsync(country, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return country.ToResponse();
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateCountryRequest request, CancellationToken ct = default)
    {
        var country = await countryRepository.GetByIdAsync(id, ct);
        if (country is null)
            return Result.Fail(DomainErrors.CountryNotFound);

        country.Name = request.Name;
        country.IsoCode = request.IsoCode;

        countryRepository.Update(country);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await countryRepository.DeleteAsync(id, ct);
        if (!deleted)
            return Result.Fail(DomainErrors.CountryNotFound);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }
}