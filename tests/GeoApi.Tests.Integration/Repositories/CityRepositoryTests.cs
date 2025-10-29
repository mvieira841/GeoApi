using FluentAssertions;
using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Requests.Cities;
using GeoApi.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeoApi.Tests.Integration.Repositories;

public class CityRepositoryTests : CustomTestWebAppFactory
{
    private readonly ICityRepository _sut;
    private readonly CancellationToken _ct = CancellationToken.None;

    public CityRepositoryTests(NestedWebAppFactory factory) : base(factory)
    {
        _sut = ServiceProvider.GetRequiredService<ICityRepository>();
    }

    private async Task<Country> SeedCountryWithCities()
    {
        var country = new Country
        {
            Name = "USA",
            IsoCode = "USA",
            Cities =
            [
                new City { Name = "New York", Latitude = 40.71m, Longitude = -74.00m },
                new City { Name = "Chicago", Latitude = 41.87m, Longitude = -87.62m },
                new City { Name = "Los Angeles", Latitude = 34.05m, Longitude = -118.24m }
            ]
        };
        await SeedAsync(country);
        return country;
    }

    [Fact]
    public async Task CreateAsync_ShouldAddCityToDatabase()
    {
        var country = new Country { Name = "Canada", IsoCode = "CAN" };
        await SeedAsync(country);
        var city = new City
        {
            Name = "Toronto",
            Latitude = 43.65m,
            Longitude = -79.38m,
            CountryId = country.Id
        };

        await _sut.CreateAsync(city, _ct);
        await DbContext.SaveChangesAsync(_ct);

        var result = await _sut.GetByIdAsync(city.Id, _ct);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Toronto");
        result.CountryId.Should().Be(country.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCorrectCity()
    {
        var country = await SeedCountryWithCities();
        var cityId = country.Cities.First(c => c.Name == "New York").Id;

        var result = await _sut.GetByIdAsync(cityId, _ct);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New York");
    }

    [Fact]
    public async Task GetAllForCountryAsync_ShouldReturnOnlyCitiesForThatCountry()
    {
        var country1 = await SeedCountryWithCities(); // USA
        var country2 = new Country
        {
            Name = "Mexico",
            IsoCode = "MEX",
            Cities = [
            new City { Name = "Mexico City", Latitude = 19.43m, Longitude = -99.13m }
        ]
        };
        await SeedAsync(country2);
        var request = new GetAllCitiesRequest { Page = 1, PageSize = 10, SortColumn = "Name", SortOrder = "ASC" };

        var result = await _sut.GetAllForCountryAsync(country1.Id, request, _ct);

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
        result.Items.Should().NotContain(c => c.Name == "Mexico City");
        result.Items.First().Name.Should().Be("Chicago");
    }

    [Fact]
    public async Task GetAllForCountryAsync_ShouldHandlePaging()
    {
        var country = await SeedCountryWithCities();
        var request = new GetAllCitiesRequest { Page = 2, PageSize = 2, SortColumn = "Name", SortOrder = "ASC" };

        var result = await _sut.GetAllForCountryAsync(country.Id, request, _ct);

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("New York");
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetAllForCountryAsync_ShouldFilterByName()
    {
        var country = await SeedCountryWithCities();
        var request = new GetAllCitiesRequest { Name = "York" };

        var result = await _sut.GetAllForCountryAsync(country.Id, request, _ct);

        result.Items.Should().ContainSingle(c => c.Name == "New York");
    }

    [Fact]
    public async Task GetAllForCountryAsync_ShouldFilterByLatitude()
    {
        var country = await SeedCountryWithCities();
        var request = new GetAllCitiesRequest { Latitude = 41.87m };

        var result = await _sut.GetAllForCountryAsync(country.Id, request, _ct);

        result.Items.Should().ContainSingle(c => c.Name == "Chicago");
    }

    [Fact]
    public async Task GetAllForCountryAsync_ShouldFilterByLongitude()
    {
        var country = await SeedCountryWithCities();
        var request = new GetAllCitiesRequest { Longitude = -118.24m };

        var result = await _sut.GetAllForCountryAsync(country.Id, request, _ct);

        result.Items.Should().ContainSingle(c => c.Name == "Los Angeles");
    }

    [Fact]
    public async Task GetAllForCountryAsync_ShouldFilterByAllFields()
    {
        var country = await SeedCountryWithCities();
        var request = new GetAllCitiesRequest
        {
            Name = "New",
            Latitude = 40.71m,
            Longitude = -74.00m
        };

        var result = await _sut.GetAllForCountryAsync(country.Id, request, _ct);

        result.Items.Should().ContainSingle(c => c.Name == "New York");
    }

    [Fact]
    public async Task Update_ShouldUpdateEntityInDatabase()
    {
        var country = await SeedCountryWithCities();
        var cityId = country.Cities.First().Id;

        var cityToUpdate = country.Cities.First();
        cityToUpdate.Name = "Updated City";
        _sut.Update(cityToUpdate);
        await DbContext.SaveChangesAsync(_ct);

        var fromDb = await DbContext.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cityId, _ct);

        fromDb.Should().NotBeNull();
        fromDb!.Name.Should().Be("Updated City");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCityFromDatabase()
    {
        var country = await SeedCountryWithCities();
        var city = country.Cities.First();
        var cityId = city.Id;
        var countryId = country.Id;

        var deleted = await _sut.DeleteAsync(cityId, _ct);
        await DbContext.SaveChangesAsync(_ct);

        deleted.Should().BeTrue();
        bool cityExists = await DbContext.Cities.AnyAsync(c => c.Id == cityId, _ct);
        int remainingCityCount = await DbContext.Cities.CountAsync(c => c.CountryId == countryId, _ct);
        bool countryExists = await DbContext.Countries.AnyAsync(c => c.Id == countryId, _ct);

        cityExists.Should().BeFalse();
        countryExists.Should().BeTrue();
        remainingCityCount.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenIdNotFound()
    {
        var deleted = await _sut.DeleteAsync(Guid.NewGuid(), _ct);

        deleted.Should().BeFalse();
    }
}