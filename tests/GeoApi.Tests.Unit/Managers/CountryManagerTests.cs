using FluentAssertions;
using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Interfaces.Managers;
using GeoApi.Abstractions.Mappers;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Countries;
using GeoApi.Abstractions.Responses.Countries;
using GeoApi.Manager.Managers;
using GeoApi.Manager.Utility;
using NSubstitute;

namespace GeoApi.Tests.Unit.Managers;

public class CountryManagerTests
{
    private readonly ICountryManager _sut;
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICountryRepository _countryRepository = Substitute.For<ICountryRepository>();
    private readonly CancellationToken _ct = CancellationToken.None;

    public CountryManagerTests()
    {
        _sut = new CountryManager(_countryRepository, _unitOfWork);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCountry_WhenCountryExists()
    {
        var countryId = Guid.NewGuid();
        var country = new Country { Id = countryId, Name = "Brazil", IsoCode = "BRA", Cities = [] };
        _countryRepository.GetByIdAsync(countryId, _ct).Returns(country);

        var result = await _sut.GetByIdAsync(countryId, _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(country.ToResponse());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenCountryDoesNotExist()
    {
        var countryId = Guid.NewGuid();
        _countryRepository.GetByIdAsync(countryId, _ct).Returns(Task.FromResult<Country?>(null));

        var result = await _sut.GetByIdAsync(countryId, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == DomainErrors.CountryNotFound);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedList_WhenCalled()
    {
        var pagedRequest = new GetAllCountriesRequest { Page = 1, PageSize = 10 };
        var countryList = new List<Country>
        {
            new() { Id = Guid.NewGuid(), Name = "Brazil", IsoCode = "BRA", Cities = [] }
        };
        var pagedList = new PagedList<Country>(countryList, 1, 10, 1);
        _countryRepository.GetAllAsync(pagedRequest, _ct).Returns(pagedList);

        var result = await _sut.GetAllAsync(pagedRequest, _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Should().BeEquivalentTo(countryList.First().ToResponse());
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenRequestIsValid()
    {
        var request = new CreateCountryRequest("Brazil", "BRA");
        Country? capturedCountry = null;
        var expectedId = Guid.NewGuid();
        _countryRepository.GetByNameAsync(request.Name, _ct).Returns(Task.FromResult<Country?>(null));
        _countryRepository.CreateAsync(Arg.Do<Country>(c => capturedCountry = c), _ct)
            .Returns(Task.FromResult(Guid.Empty));
        _unitOfWork.SaveChangesAsync(_ct).Returns(callInfo =>
        {
            if (capturedCountry != null)
            {
                capturedCountry.Id = expectedId;
            }
            return Task.FromResult(1);
        });

        var result = await _sut.CreateAsync(request, _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Match<CountryResponse>(r =>
            r.Name == request.Name &&
            r.IsoCode == request.IsoCode &&
            r.Id == expectedId
        );
        capturedCountry.Should().NotBeNull();
        capturedCountry!.Name.Should().Be(request.Name);
        capturedCountry.IsoCode.Should().Be(request.IsoCode);
        await _unitOfWork.Received(1).SaveChangesAsync(_ct);
    }


    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenCountryNameAlreadyExists()
    {
        var request = new CreateCountryRequest("Brazil", "BRA");
        var existingCountry = new Country { Id = Guid.NewGuid(), Name = "Brazil", IsoCode = "BRA" };
        _countryRepository.GetByNameAsync(request.Name, _ct).Returns(existingCountry);

        var result = await _sut.CreateAsync(request, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == DomainErrors.CountryConflict);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnSuccess_WhenRequestIsValid()
    {
        var countryId = Guid.NewGuid();
        var request = new UpdateCountryRequest("Brazil (Updated)", "BRZ");
        var existingCountry = new Country { Id = countryId, Name = "Brazil", IsoCode = "BRA" };
        _countryRepository.GetByIdAsync(countryId, _ct).Returns(existingCountry);

        var result = await _sut.UpdateAsync(countryId, request, _ct);

        result.IsSuccess.Should().BeTrue();
        _countryRepository.Received(1).Update(Arg.Is<Country>(c =>
            c.Id == countryId &&
            c.Name == "Brazil (Updated)" &&
            c.IsoCode == "BRZ"
        ));
        await _unitOfWork.Received(1).SaveChangesAsync(_ct);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFailure_WhenCountryDoesNotExist()
    {
        var countryId = Guid.NewGuid();
        var request = new UpdateCountryRequest("Brazil (Updated)", "BRZ");
        _countryRepository.GetByIdAsync(countryId, _ct).Returns(Task.FromResult<Country?>(null));

        var result = await _sut.UpdateAsync(countryId, request, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == DomainErrors.CountryNotFound);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenCountryExists()
    {
        var countryId = Guid.NewGuid();
        _countryRepository.DeleteAsync(countryId, _ct).Returns(true);

        var result = await _sut.DeleteAsync(countryId, _ct);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(_ct);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFailure_WhenCountryDoesNotExist()
    {
        var countryId = Guid.NewGuid();
        _countryRepository.DeleteAsync(countryId, _ct).Returns(false);

        var result = await _sut.DeleteAsync(countryId, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == DomainErrors.CountryNotFound);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}