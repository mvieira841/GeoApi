using FluentAssertions;
using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Requests.Countries;
using GeoApi.Tests.Integration.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeoApi.Tests.Integration.Repositories;

public class CountryRepositoryTests
    : CustomTestWebAppFactory
{
    private readonly ICountryRepository _sut;
    private readonly CancellationToken _ct = CancellationToken.None;

    public CountryRepositoryTests(NestedWebAppFactory factory) : base(factory)
    {
        _sut = ServiceProvider.GetRequiredService<ICountryRepository>();
    }

    [Fact]
    public async Task CreateAsync_ShouldAddCountryToDatabase()
    {
        var country = new Country { Name = "Canada", IsoCode = "CAN" };

        await _sut.CreateAsync(country, _ct);
        await DbContext.SaveChangesAsync(_ct);

        var result = await _sut.GetByIdAsync(country.Id, _ct);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Canada");
        result.IsoCode.Should().Be("CAN");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCorrectCountry_WhenExists()
    {
        var country = new Country { Name = "Japan", IsoCode = "JPN" };
        await SeedAsync(country);

        var result = await _sut.GetByIdAsync(country.Id, _ct);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Japan");
    }

    [Fact]
    public async Task GetByNameAsync_ShouldReturnCorrectCountry_WhenExists()
    {
        await SeedAsync(new Country { Name = "Brazil", IsoCode = "BRA" });

        var result = await _sut.GetByNameAsync("Brazil", _ct);

        result.Should().NotBeNull();
        result!.IsoCode.Should().Be("BRA");
    }

    [Fact]
    public async Task GetByNameAsync_ShouldReturnNull_WhenNotFound()
    {
        var result = await _sut.GetByNameAsync("NonExistent", _ct);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedList_WithCorrectSorting()
    {
        await SeedAsync(
            new Country { Name = "Brazil", IsoCode = "BRA" },
            new Country { Name = "Argentina", IsoCode = "ARG" },
            new Country { Name = "USA", IsoCode = "USA" }
        );

        var request = new GetAllCountriesRequest
        {
            Page = 1,
            PageSize = 10,
            SortColumn = "Name",
            SortOrder = "DESC"
        };

        var result = await _sut.GetAllAsync(request, _ct);

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
        result.Items.First().Name.Should().Be("USA");
        result.Items.Last().Name.Should().Be("Argentina");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedList_WithCorrectPaging()
    {
        await SeedAsync(
            new Country { Name = "A", IsoCode = "A" },
            new Country { Name = "B", IsoCode = "B" },
            new Country { Name = "C", IsoCode = "C" }
        );

        var request = new GetAllCountriesRequest
        {
            Page = 2,
            PageSize = 1,
            SortColumn = "Name",
            SortOrder = "ASC"
        };

        var result = await _sut.GetAllAsync(request, _ct);

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("B");
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_ShouldUseDefaultSort_WhenNoSortIsProvided()
    {
        await SeedAsync(
            new Country { Id = new Guid("11111111-1111-1111-1111-111111111111"), Name = "Z", IsoCode = "Z" },
            new Country { Id = new Guid("00000000-0000-0000-0000-000000000000"), Name = "A", IsoCode = "A" }
        );
        var request = new GetAllCountriesRequest { Page = 1, PageSize = 10 };

        var result = await _sut.GetAllAsync(request, _ct);

        result.Items.Should().HaveCount(2);
        result.Items.First().Name.Should().Be("A");
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByName_WhenNameIsProvided()
    {
        await SeedAsync(
            new Country { Name = "Brazil", IsoCode = "BRA" },
            new Country { Name = "Argentina", IsoCode = "ARG" }
        );
        var request = new GetAllCountriesRequest { Name = "Bra" };

        var result = await _sut.GetAllAsync(request, _ct);

        result.Items.Should().ContainSingle(c => c.Name == "Brazil");
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByIsoCode_WhenIsoCodeIsProvided()
    {
        await SeedAsync(
            new Country { Name = "Brazil", IsoCode = "BRA" },
            new Country { Name = "Argentina", IsoCode = "ARG" }
        );
        var request = new GetAllCountriesRequest { IsoCode = "AR" };

        var result = await _sut.GetAllAsync(request, _ct);

        result.Items.Should().ContainSingle(c => c.Name == "Argentina");
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByNameAndIsoCode_WhenBothAreProvided()
    {
        await SeedAsync(
            new Country { Name = "Brazil", IsoCode = "BRA" },
            new Country { Name = "Argentina", IsoCode = "ARG" }
        );
        var request = new GetAllCountriesRequest { Name = "Arg", IsoCode = "ARG" };

        var result = await _sut.GetAllAsync(request, _ct);

        result.Items.Should().ContainSingle(c => c.Name == "Argentina");
    }

    [Fact]
    public async Task Update_ShouldUpdateEntityInDatabase()
    {
        var country = new Country { Name = "Germany", IsoCode = "DEU" };
        await SeedAsync(country);

        country.Name = "Germany (Updated)";
        _sut.Update(country);
        await DbContext.SaveChangesAsync(_ct);

        var fromDb = await DbContext.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == country.Id, _ct);
        fromDb.Should().NotBeNull();
        fromDb!.Name.Should().Be("Germany (Updated)");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCountryAndCities_WhenCascadeDeleteIsConfigured()
    {
        var country = new Country
        {
            Name = "Japan",
            IsoCode = "JPN",
            Cities = [new City { Name = "Tokyo", Latitude = 35.68m, Longitude = 139.69m }]
        };
        await SeedAsync(country);
        var cityId = country.Cities.First().Id;

        var deleted = await _sut.DeleteAsync(country.Id, _ct);
        await DbContext.SaveChangesAsync(_ct);

        deleted.Should().BeTrue();
        bool countryExists = await DbContext.Countries.AnyAsync(c => c.Id == country.Id, _ct);
        bool cityExists = await DbContext.Cities.AnyAsync(c => c.Id == cityId, _ct);

        countryExists.Should().BeFalse();
        cityExists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenIdNotFound()
    {
        var deleted = await _sut.DeleteAsync(Guid.NewGuid(), _ct);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenExists()
    {
        var country = new Country { Name = "Spain", IsoCode = "ESP" };
        await SeedAsync(country);

        var result = await _sut.ExistsAsync(country.Id, _ct);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenNotExists()
    {
        var result = await _sut.ExistsAsync(Guid.NewGuid(), _ct);

        result.Should().BeFalse();
    }
}