using FluentAssertions;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Cities;
using GeoApi.Abstractions.Responses.Cities;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using static GeoApi.Tests.Acceptance.CustomWebApplicationFactory;

namespace GeoApi.Tests.Acceptance.Endpoints;

public class CitiesEndpointsTests(NestedWebAppFactory factory) : CustomWebApplicationFactory(factory)
{
    #region CREATE (POST)
    [Fact]
    public async Task POST_Cities_ShouldCreateCity_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var createUrl = GetCreateCityUrl(countryId);
        var uniqueName = $"TestCity-{Guid.NewGuid()}";
        var request = new CreateCityRequest(uniqueName, 40.7128m, -74.0060m);

        // Act
        var response = await client.PostAsJsonAsync(createUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var cityResponse = await response.Content.ReadFromJsonAsync<CityResponse>();
        cityResponse!.Name.Should().Be(uniqueName);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_Cities_ShouldReturn403Forbidden_WhenAuthenticatedAsUser()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var createUrl = GetCreateCityUrl(countryId);
        var request = new CreateCityRequest("TestCity", 0, 0);

        // Act
        var response = await client.PostAsJsonAsync(createUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task POST_Cities_ShouldReturn404NotFound_WhenCountryIdDoesNotExist()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var createUrl = GetCreateCityUrl(Guid.NewGuid());
        var request = new CreateCityRequest("TestCity", 0, 0);

        // Act
        var response = await client.PostAsJsonAsync(createUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Cities_ShouldReturnValidationProblem_WhenRequestIsInvalid()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var createUrl = GetCreateCityUrl(countryId);
        var request = new CreateCityRequest("", -200, -200);

        // Act
        var response = await client.PostAsJsonAsync(createUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKeys("Name", "Latitude", "Longitude");
        problem.Errors["Name"][0].Should().Be("'Name' must not be empty.");
        problem.Errors["Latitude"][0].Should().Be("'Latitude' must be between -90 and 90. You entered -200.");
        problem.Errors["Longitude"][0].Should().Be("'Longitude' must be between -180 and 180. You entered -200.");
    }

    [Fact]
    public async Task POST_Cities_ShouldReturnValidationProblem_WhenNameExceedsMaxLength()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var createUrl = GetCreateCityUrl(countryId);
        var longName = new string('a', 101);
        var request = new CreateCityRequest(longName, 40.7128m, -74.0060m);

        // Act
        var response = await client.PostAsJsonAsync(createUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("Name");
        problem.Errors["Name"][0].Should().Contain("must be 100 characters or fewer");
    }
    #endregion

    #region READ (GET)
    [Fact]
    public async Task GET_Cities_ShouldReturnDefaultSortedList_WhenNoQueryIsProvided()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var getAllUrl = GetAllCitiesUrl(countryId);

        // Act
        var response = await client.GetAsync(getAllUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CityResponse>>();
        pagedList!.Items.Should().NotBeEmpty();
        pagedList.Items.First().Name.Should().Be("Chicago");
    }

    [Fact]
    public async Task GET_Cities_ShouldReturnSortedList_WhenValidSortColumnIsProvided()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var getAllUrl = $"{GetAllCitiesUrl(countryId)}?sortColumn=Latitude&sortOrder=DESC";

        // Act
        var response = await client.GetAsync(getAllUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CityResponse>>();
        pagedList!.Items.Should().NotBeEmpty();
        pagedList.Items.First().Name.Should().Be("Chicago");
    }

    [Fact]
    public async Task GET_Cities_ShouldReturnFilteredList_WhenNameFilterIsProvided()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var url = $"{GetAllCitiesUrl(countryId)}?name=Chicago";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CityResponse>>();
        pagedList!.Items.Should().ContainSingle(c => c.Name == "Chicago");
    }

    [Fact]
    public async Task GET_Cities_ShouldReturnFilteredList_WhenLatLonFilterIsProvided()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var url = $"{GetAllCitiesUrl(countryId)}?latitude=40.7128&longitude=-74.0060";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CityResponse>>();
        pagedList!.Items.Should().ContainSingle(c => c.Name == "New York");
    }

    [Fact]
    public async Task GET_Cities_ShouldReturnEmptyList_WhenFilterMatchesNothing()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var url = $"{GetAllCitiesUrl(countryId)}?name=NonExistentCity";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CityResponse>>();
        pagedList!.Items.Should().BeEmpty();
    }

    // Consolidated query validation
    [Theory]
    [InlineData("page=0", "Page")]
    [InlineData("pageSize=0", "PageSize")]
    [InlineData("pageSize=101", "PageSize")]
    [InlineData("sortOrder=foo", "SortOrder")]
    [InlineData("latitude=91", "Latitude")]
    [InlineData("latitude=-91", "Latitude")]
    [InlineData("longitude=181", "Longitude")]
    [InlineData("longitude=-181", "Longitude")]
    [InlineData("sortColumn=InvalidProp", "SortColumn")]
    public async Task GET_Cities_ShouldReturnValidationProblem_WhenQueryIsInvalid(string query, string errorKey)
    {
        // Arrange
        var client = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var url = $"{GetAllCitiesUrl(countryId)}?{query}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey(errorKey);

        if (errorKey == "SortColumn")
        {
            // Corrected: Reflects actual properties on GetAllCitiesRequest
            problem.Errors[errorKey][0].Should().Contain("SortColumn must be one of: Id, Name, Country, Latitude, Longitude");
        }
    }

    [Fact]
    public async Task GET_Cities_ShouldReturnValidationProblem_WhenNameFilterExceedsMaxLength()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var longName = new string('a', 101);
        var url = $"{GetAllCitiesUrl(countryId)}?name={longName}";

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("Name");
        problem.Errors["Name"][0].Should().Contain("must be 100 characters or fewer");
    }

    [Fact]
    public async Task GET_Cities_ShouldReturn404NotFound_WhenCountryIdDoesNotExist()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var getAllUrl = GetAllCitiesUrl(Guid.NewGuid());

        // Act
        var response = await client.GetAsync(getAllUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_CityById_ShouldReturnCity_WhenAuthenticated()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var cityId = await GetSeededCityIdAsync(client, countryId, "New York");
        var getByIdUrl = GetCityByIdUrl(countryId, cityId);

        // Act
        var cityResponse = await client.GetAsync(getByIdUrl);

        // Assert
        cityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var city = await cityResponse.Content.ReadFromJsonAsync<CityResponse>();
        city!.Name.Should().Be("New York");
    }

    [Fact]
    public async Task GET_CityById_ShouldReturn404NotFound_WhenCityIdDoesNotExist()
    {
        // Arrange
        var client = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var getByIdUrl = GetCityByIdUrl(countryId, Guid.NewGuid());

        // Act
        var response = await client.GetAsync(getByIdUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    #endregion

    #region UPDATE (PUT)
    [Fact]
    public async Task PUT_Cities_ShouldReturn204NoContent_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var testCity = await CreateTestCityAsync(client, countryId);
        var request = new UpdateCityRequest($"UpdatedCity-{Guid.NewGuid()}", 1, 1);
        var updateUrl = GetUpdateCityUrl(countryId, testCity.Id);

        // Act
        var response = await client.PutAsJsonAsync(updateUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PUT_Cities_ShouldReturn404NotFound_WhenCityIdDoesNotExist()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var request = new UpdateCityRequest("Will Fail", 1, 1);
        var updateUrl = GetUpdateCityUrl(countryId, Guid.NewGuid());

        // Act
        var response = await client.PutAsJsonAsync(updateUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_Cities_ShouldReturn403Forbidden_WhenAuthenticatedAsUser()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var userClient = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(adminClient, "USA");
        var testCity = await CreateTestCityAsync(adminClient, countryId);
        var request = new UpdateCityRequest("Will Fail", 1, 1);
        var updateUrl = GetUpdateCityUrl(countryId, testCity.Id);

        // Act
        var response = await userClient.PutAsJsonAsync(updateUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PUT_Cities_ShouldReturnValidationProblem_WhenRequestIsInvalid()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var countryId = await GetSeededCountryIdAsync(client);
        var testCity = await CreateTestCityAsync(client, countryId);
        var request = new UpdateCityRequest("", -200, 200);
        var updateUrl = GetUpdateCityUrl(countryId, testCity.Id);

        // Act
        var response = await client.PutAsJsonAsync(updateUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKeys("Name", "Latitude", "Longitude");
        problem.Errors["Name"][0].Should().Be("'Name' must not be empty.");
        problem.Errors["Latitude"][0].Should().Be("'Latitude' must be between -90 and 90. You entered -200.");
        problem.Errors["Longitude"][0].Should().Be("'Longitude' must be between -180 and 180. You entered 200.");
    }

    [Fact]
    public async Task PUT_Cities_ShouldReturnValidationProblem_WhenNameExceedsMaxLength()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var countryId = await GetSeededCountryIdAsync(client);
        var testCity = await CreateTestCityAsync(client, countryId);
        var longName = new string('a', 101);
        var request = new UpdateCityRequest(longName, 1, 1);
        var updateUrl = GetUpdateCityUrl(countryId, testCity.Id);

        // Act
        var response = await client.PutAsJsonAsync(updateUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("Name");
        problem.Errors["Name"][0].Should().Contain("must be 100 characters or fewer");
    }
    #endregion

    #region DELETE
    [Fact]
    public async Task DELETE_Cities_ShouldReturn204NoContent_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var testCity = await CreateTestCityAsync(client, countryId);
        var deleteUrl = GetDeleteCityUrl(countryId, testCity.Id);

        // Act
        var deleteResponse = await client.DeleteAsync(deleteUrl);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_Cities_ShouldReturn404NotFound_WhenCityIdDoesNotExist()
    {
        // Arrange
        var client = await GetAdminClientAsync();
        var countryId = await GetSeededCountryIdAsync(client, "USA");
        var deleteUrl = GetDeleteCityUrl(countryId, Guid.NewGuid());

        // Act
        var deleteResponse = await client.DeleteAsync(deleteUrl);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_Cities_ShouldReturn403Forbidden_WhenAuthenticatedAsUser()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var userClient = await GetUserClientAsync();
        var countryId = await GetSeededCountryIdAsync(adminClient, "USA");
        var testCity = await CreateTestCityAsync(adminClient, countryId);
        var deleteUrl = GetDeleteCityUrl(countryId, testCity.Id);

        // Act
        var deleteResponse = await userClient.DeleteAsync(deleteUrl);

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    #endregion
}