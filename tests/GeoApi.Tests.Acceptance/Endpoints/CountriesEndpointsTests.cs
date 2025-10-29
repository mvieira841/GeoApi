using FluentAssertions;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Countries;
using GeoApi.Abstractions.Responses.Countries;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using static GeoApi.Tests.Acceptance.CustomWebApplicationFactory;

namespace GeoApi.Tests.Acceptance.Endpoints;

public class CountriesEndpointsTests(NestedWebAppFactory factory) : CustomWebApplicationFactory(factory)
{
    #region CREATE (POST)
    [Fact]
    public async Task POST_Countries_ShouldCreateCountry_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var uniqueName = $"TestCountry-{Guid.NewGuid()}";
        var request = new CreateCountryRequest(uniqueName, "TCY");

        // Act
        var response = await client.PostAsJsonAsync(CreateCountryUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var countryResponse = await response.Content.ReadFromJsonAsync<CountryResponse>();
        countryResponse.Should().NotBeNull();
        countryResponse!.Name.Should().Be(uniqueName);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_Countries_ShouldReturn401Unauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = GetPublicClient(); // Use base method
        var request = new CreateCountryRequest("Test", "TST");

        // Act
        var response = await client.PostAsJsonAsync(CreateCountryUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Countries_ShouldReturn403Forbidden_WhenAuthenticatedAsUser()
    {
        // Arrange
        var client = await GetUserClientAsync(); // Use base method
        var request = new CreateCountryRequest("Test", "TST");

        // Act
        var response = await client.PostAsJsonAsync(CreateCountryUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task POST_Countries_ShouldReturnValidationProblem_WhenRequestIsEmpty()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var request = new CreateCountryRequest("", "");

        // Act
        var response = await client.PostAsJsonAsync(CreateCountryUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKeys("Name", "IsoCode");
        problem.Errors["Name"][0].Should().Be("'Name' must not be empty.");
        problem.Errors["IsoCode"][0].Should().Be("'Iso Code' must not be empty.");
    }

    [Fact]
    public async Task POST_Countries_ShouldReturnValidationProblem_WhenNameExceedsMaxLength()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var longName = new string('a', 101); // 101 chars
        var request = new CreateCountryRequest(longName, "USA");

        // Act
        var response = await client.PostAsJsonAsync(CreateCountryUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("Name");
        problem.Errors["Name"][0].Should().Contain("must be 100 characters or fewer");
    }

    [Fact]
    public async Task POST_Countries_ShouldReturnValidationProblem_WhenIsoCodeIsInvalidLength()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var request = new CreateCountryRequest("Valid Name", "US"); // Too short

        // Act
        var response = await client.PostAsJsonAsync(CreateCountryUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("IsoCode");
        problem.Errors["IsoCode"][0].Should().Contain("must be 3 characters");
    }

    [Fact]
    public async Task POST_Countries_ShouldReturnConflict_WhenCountryNameExists()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var request = new CreateCountryRequest("USA", "US1"); // USA is seeded

        // Act
        var response = await client.PostAsJsonAsync(CreateCountryUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
    #endregion

    #region READ (GET)
    [Fact]
    public async Task GET_Countries_ShouldReturnSortedList_WhenValidSortColumnIsProvided()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var url = $"{GetAllCountriesUrl}?page=1&pageSize=10&sortColumn=IsoCode&sortOrder=DESC";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CountryResponse>>();
        pagedList.Should().NotBeNull();
        pagedList!.Items.Should().NotBeEmpty();
        pagedList.Items.Should().BeInDescendingOrder(c => c.IsoCode);
        pagedList.Items.First().Name.Should().Be("South Africa");
    }

    [Fact]
    public async Task GET_Countries_ShouldReturnFilteredList_WhenNameFilterIsProvided()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var url = $"{GetAllCountriesUrl}?name=Brazil";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CountryResponse>>();
        pagedList!.Items.Should().ContainSingle(c => c.Name == "Brazil");
    }

    [Fact]
    public async Task GET_Countries_ShouldReturnFilteredList_WhenIsoCodeFilterIsProvided()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var url = $"{GetAllCountriesUrl}?isoCode=JPN";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CountryResponse>>();
        pagedList!.Items.Should().ContainSingle(c => c.Name == "Japan");
    }

    [Fact]
    public async Task GET_Countries_ShouldReturnEmptyList_WhenFilterMatchesNothing()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var url = $"{GetAllCountriesUrl}?name=NonExistentCountry";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CountryResponse>>();
        pagedList!.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("pageSize=0", "PageSize")]
    [InlineData("pageSize=101", "PageSize")]
    [InlineData("sortOrder=foo", "SortOrder")]
    [InlineData("sortColumn=InvalidProp", "SortColumn")]
    [InlineData("isoCode=USAA", "IsoCode")]
    public async Task GET_Countries_ShouldReturnValidationProblem_WhenQueryIsInvalid(string query, string errorKey)
    {
        // Arrange
        var client = await GetUserClientAsync();
        var url = $"{GetAllCountriesUrl}?{query}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey(errorKey);

        // Check the error message for invalid SortColumn
        if (errorKey == "SortColumn")
        {
            problem.Errors[errorKey][0].Should().Contain("SortColumn must be one of: Id, Name, IsoCode");
        }
    }

    [Fact]
    public async Task GET_Countries_ShouldReturnValidationProblem_WhenNameFilterExceedsMaxLength()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var longName = new string('a', 101);
        var url = $"{GetAllCountriesUrl}?name={longName}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("Name");
        problem.Errors["Name"][0].Should().Contain("must be 100 characters or fewer");
    }

    [Fact]
    public async Task GET_Countries_ShouldReturnDefaultSortedList_WhenNoQueryIsProvided()
    {
        // Arrange
        var client = await GetUserClientAsync();

        // Act
        var response = await client.GetAsync(GetAllCountriesUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CountryResponse>>();
        pagedList!.Page.Should().Be(1);
        pagedList.PageSize.Should().Be(10);
        pagedList.Items.Should().NotBeEmpty();
        pagedList.Items.First().Name.Should().Be("Australia");
    }

    [Fact]
    public async Task GET_Countries_ShouldReturn401Unauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = GetPublicClient();

        // Act
        var response = await client.GetAsync(GetAllCountriesUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_CountryById_ShouldReturnCountry_WhenAuthenticated()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var usaId = await GetSeededCountryIdAsync(client, "USA");
        var url = GetGetCountryByIdUrl(usaId);

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var country = await response.Content.ReadFromJsonAsync<CountryResponse>();
        country!.Name.Should().Be("USA");
    }

    [Fact]
    public async Task GET_CountryById_ShouldReturn404NotFound_WhenIdDoesNotExist()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var url = GetGetCountryByIdUrl(Guid.NewGuid());

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    #endregion

    #region UPDATE (PUT)
    [Fact]
    public async Task PUT_Countries_ShouldReturn204NoContent_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var testCountry = await CreateTestCountryAsync(client);
        var request = new UpdateCountryRequest($"Updated-{Guid.NewGuid()}", "UPD");
        var url = GetUpdateCountryUrl(testCountry.Id);

        // Act
        var response = await client.PutAsJsonAsync(url, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PUT_Countries_ShouldReturn403Forbidden_WhenAuthenticatedAsUser()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var userClient = await GetUserClientAsync();
        var testCountry = await CreateTestCountryAsync(adminClient);
        var request = new UpdateCountryRequest("No-Permissions", "NPM");
        var url = GetUpdateCountryUrl(testCountry.Id);

        // Act
        var response = await userClient.PutAsJsonAsync(url, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PUT_Countries_ShouldReturn404NotFound_WhenIdDoesNotExist()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var request = new UpdateCountryRequest("Will Fail", "WFA");
        var url = GetUpdateCountryUrl(Guid.NewGuid());

        // Act
        var response = await client.PutAsJsonAsync(url, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Countries_ShouldReturnValidationProblem_WhenRequestIsEmpty()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var testCountry = await CreateTestCountryAsync(client);
        var request = new UpdateCountryRequest("", "");
        var url = GetUpdateCountryUrl(testCountry.Id);

        // Act
        var response = await client.PutAsJsonAsync(url, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKeys("Name", "IsoCode");
        problem.Errors["Name"][0].Should().Be("'Name' must not be empty.");
        problem.Errors["IsoCode"][0].Should().Be("'Iso Code' must not be empty.");
    }

    [Fact]
    public async Task PUT_Countries_ShouldReturnValidationProblem_WhenNameExceedsMaxLength()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var testCountry = await CreateTestCountryAsync(client);
        var longName = new string('a', 101); // 101 chars
        var request = new UpdateCountryRequest(longName, "USA");
        var url = GetUpdateCountryUrl(testCountry.Id);

        // Act
        var response = await client.PutAsJsonAsync(url, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("Name");
        problem.Errors["Name"][0].Should().Contain("must be 100 characters or fewer");
    }

    [Fact]
    public async Task PUT_Countries_ShouldReturnValidationProblem_WhenIsoCodeIsInvalidLength()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var testCountry = await CreateTestCountryAsync(client);
        var request = new UpdateCountryRequest("Valid Name", "US");
        var url = GetUpdateCountryUrl(testCountry.Id);

        // Act
        var response = await client.PutAsJsonAsync(url, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("IsoCode");
        problem.Errors["IsoCode"][0].Should().Contain("must be 3 characters");
    }
    #endregion

    #region DELETE
    [Fact]
    public async Task DELETE_Countries_ShouldReturn204NoContent_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var testCountry = await CreateTestCountryAsync(client);
        var url = GetDeleteCountryUrl(testCountry.Id);

        // Act
        var deleteResponse = await client.DeleteAsync(url);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_Countries_ShouldReturn403Forbidden_WhenAuthenticatedAsUser()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var userClient = await GetUserClientAsync();
        var testCountry = await CreateTestCountryAsync(adminClient);
        var url = GetDeleteCountryUrl(testCountry.Id);

        // Act
        var response = await userClient.DeleteAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DELETE_Countries_ShouldReturn404NotFound_WhenIdDoesNotExist()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var url = GetDeleteCountryUrl(Guid.NewGuid());

        // Act
        var response = await client.DeleteAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    #endregion
}