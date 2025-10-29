using FluentAssertions;
using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Interfaces.Managers;
using GeoApi.Abstractions.Mappers;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Cities;
using GeoApi.Abstractions.Responses.Cities;
using GeoApi.Manager.Managers;
using GeoApi.Manager.Utility;
using NSubstitute;

namespace GeoApi.Tests.Unit.Managers;

public class CityManagerTests
{
    private readonly ICityManager _sut;
    private readonly ICountryRepository _countryRepository = Substitute.For<ICountryRepository>();
    private readonly ICityRepository _cityRepository = Substitute.For<ICityRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CancellationToken _ct = CancellationToken.None;

    public CityManagerTests()
    {
        _sut = new CityManager(_cityRepository, _countryRepository, _unitOfWork);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCity_WhenCityExists()
    {
        var cityId = Guid.NewGuid();
        var city = new City { Id = cityId, Name = "Rio de Janeiro", Latitude = -22.9m, Longitude = -43.1m };
        _cityRepository.GetByIdAsync(cityId, _ct).Returns(city);

        var result = await _sut.GetByIdAsync(cityId, _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(city.ToResponse());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenCityDoesNotExist()
    {
        var cityId = Guid.NewGuid();
        _cityRepository.GetByIdAsync(cityId, _ct).Returns(Task.FromResult<City?>(null));

        var result = await _sut.GetByIdAsync(cityId, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == DomainErrors.CityNotFound);
    }

    [Fact]
    public async Task GetAllForCountryAsync_ShouldReturnPagedList_WhenCountryExists()
    {
        var countryId = Guid.NewGuid();
        var pagedRequest = new GetAllCitiesRequest { Page = 1, PageSize = 10, Name = "Rio" };
        var cityList = new List<City>
        {
            new() { Id = Guid.NewGuid(), Name = "Rio de Janeiro", CountryId = countryId, Latitude = -22.9m, Longitude = -43.1m }
        };
        var pagedList = new PagedList<City>(cityList, 1, 10, 1);
        _countryRepository.ExistsAsync(countryId, _ct).Returns(true);
        _cityRepository.GetAllForCountryAsync(countryId, pagedRequest, _ct).Returns(pagedList);

        var result = await _sut.GetAllForCountryAsync(countryId, pagedRequest, _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Should().BeEquivalentTo(cityList.First().ToResponse());
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalCount.Should().Be(1);

        await _cityRepository.Received(1).GetAllForCountryAsync(
            countryId,
            Arg.Is<GetAllCitiesRequest>(r => r.Name == "Rio"),
            _ct);
    }

    [Fact]
    public async Task GetAllForCountryAsync_ShouldReturnFailure_WhenCountryDoesNotExist()
    {
        var countryId = Guid.NewGuid();
        var pagedRequest = new GetAllCitiesRequest { Page = 1, PageSize = 10 };
        _countryRepository.ExistsAsync(countryId, _ct).Returns(false);

        var result = await _sut.GetAllForCountryAsync(countryId, pagedRequest, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == DomainErrors.CountryNotFound);
        await _cityRepository.DidNotReceiveWithAnyArgs().GetAllForCountryAsync(default, Arg.Any<GetAllCitiesRequest>(), default);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCity_WhenRequestIsValidAndCountryExists()
    {
        var countryId = Guid.NewGuid();
        var expectedCityId = Guid.NewGuid();
        var request = new CreateCityRequest("Rio de Janeiro", -22.9068m, -43.1729m);
        City? capturedCity = null;
        _countryRepository.ExistsAsync(countryId, _ct).Returns(true);
        _cityRepository.CreateAsync(Arg.Do<City>(c => capturedCity = c), _ct)
            .Returns(Task.FromResult(Guid.Empty));
        _unitOfWork.SaveChangesAsync(_ct).Returns(callInfo =>
        {
            if (capturedCity != null)
            {
                capturedCity.Id = expectedCityId;
            }
            return Task.FromResult(1);
        });

        var result = await _sut.CreateAsync(countryId, request, _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Match<CityResponse>(r =>
            r.Name == request.Name &&
            r.Id == expectedCityId &&
            r.Latitude == request.Latitude &&
            r.Longitude == request.Longitude &&
            r.CountryId == countryId
        );
        capturedCity.Should().NotBeNull();
        capturedCity!.Name.Should().Be(request.Name);
        capturedCity.Latitude.Should().Be(request.Latitude);
        capturedCity.Longitude.Should().Be(request.Longitude);
        capturedCity.CountryId.Should().Be(countryId);
        await _unitOfWork.Received(1).SaveChangesAsync(_ct);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenCountryDoesNotExist()
    {
        var countryId = Guid.NewGuid();
        var request = new CreateCityRequest("Rio de Janeiro", -22.9068m, -43.1729m);
        _countryRepository.ExistsAsync(countryId, _ct).Returns(false);

        var result = await _sut.CreateAsync(countryId, request, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == DomainErrors.CountryNotFound);
        await _cityRepository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnSuccess_WhenRequestIsValid()
    {
        var cityId = Guid.NewGuid();
        var request = new UpdateCityRequest("São Paulo", -23.5505m, -46.6333m);
        var existingCity = new City { Id = cityId, Name = "SP", Latitude = 0, Longitude = 0 };
        _cityRepository.GetByIdAsync(cityId, _ct).Returns(existingCity);

        var result = await _sut.UpdateAsync(cityId, request, _ct);

        result.IsSuccess.Should().BeTrue();
        _cityRepository.Received(1).Update(Arg.Is<City>(c =>
            c.Id == cityId &&
            c.Name == "São Paulo" &&
            c.Latitude == -23.5505m &&
            c.Longitude == -46.6333m
        ));
        await _unitOfWork.Received(1).SaveChangesAsync(_ct);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenCityDoesNotExist()
    {
        var cityId = Guid.NewGuid();
        var request = new UpdateCityRequest("São Paulo", -23.5505m, -46.6333m);
        _cityRepository.GetByIdAsync(cityId, _ct).Returns(Task.FromResult<City?>(null));

        var result = await _sut.UpdateAsync(cityId, request, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == DomainErrors.CityNotFound);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenCityExists()
    {
        var cityId = Guid.NewGuid();
        _cityRepository.DeleteAsync(cityId, _ct).Returns(true);

        var result = await _sut.DeleteAsync(cityId, _ct);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(_ct);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenCityDoesNotExist()
    {
        var cityId = Guid.NewGuid();
        _cityRepository.DeleteAsync(cityId, _ct).Returns(false);

        var result = await _sut.DeleteAsync(cityId, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == DomainErrors.CityNotFound);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}