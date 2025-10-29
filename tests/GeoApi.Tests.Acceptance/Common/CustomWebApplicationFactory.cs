using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Auth;
using GeoApi.Abstractions.Requests.Cities;
using GeoApi.Abstractions.Requests.Countries;
using GeoApi.Abstractions.Responses.Auth;
using GeoApi.Abstractions.Responses.Cities;
using GeoApi.Abstractions.Responses.Countries;
using GeoApi.Access.Persistence;
using GeoApi.Access.Persistence.Context;
using GeoApi.Host.ApiHelpers;
using GeoApi.Host.Endpoints.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Testcontainers.MsSql;

namespace GeoApi.Tests.Acceptance;

public abstract class CustomWebApplicationFactory : IAsyncLifetime, IClassFixture<CustomWebApplicationFactory.NestedWebAppFactory>
{
    private readonly NestedWebAppFactory _factory;
    private HttpClient? _adminClient;
    private HttpClient? _userClient;
    protected static readonly string BaseApi = $"/api/{ApiVersionSet.CurrentVersionString}";
    protected static readonly string LoginUrl = $"{BaseApi}{AuthEndpointsConstants.Paths.Main}{AuthEndpointsConstants.Paths.Login}";
    protected static readonly string CountriesBaseUrl = $"{BaseApi}{CountriesEndpointsConstants.Paths.Main}";
    protected static readonly string CreateCountryUrl = $"{CountriesBaseUrl}{CountriesEndpointsConstants.Paths.Create}";
    protected static readonly string GetAllCountriesUrl = $"{CountriesBaseUrl}{CountriesEndpointsConstants.Paths.GetAll}";

    protected CustomWebApplicationFactory(NestedWebAppFactory factory)
    {
        _factory = factory;
    }

    protected async Task<HttpClient> GetAdminClientAsync()
    {
        return _adminClient ??= await CreateAuthenticatedClientAsync(DataSeeder.AdminUserName, DataSeeder.AdminPassword);
    }

    protected async Task<HttpClient> GetUserClientAsync()
    {
        return _userClient ??= await CreateAuthenticatedClientAsync(DataSeeder.UserUserName, DataSeeder.UserPassword);
    }

    protected HttpClient GetPublicClient()
    {
        return _factory.CreateClient();
    }

    protected async Task<CountryResponse> CreateTestCountryAsync(HttpClient client)
    {
        var uniqueName = $"TestCountry-{Guid.NewGuid()}";
        var isoCode = uniqueName.Substring(uniqueName.Length - 3).ToUpper();
        var request = new CreateCountryRequest(uniqueName, isoCode);
        var response = await client.PostAsJsonAsync(CreateCountryUrl, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CountryResponse>())!;
    }

    protected async Task<CityResponse> CreateTestCityAsync(HttpClient client, Guid countryId)
    {
        var createUrl = GetCreateCityUrl(countryId);
        var uniqueName = $"TestCity-{Guid.NewGuid()}";
        var request = new CreateCityRequest(uniqueName, 1, 1);
        var response = await client.PostAsJsonAsync(createUrl, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CityResponse>())!;
    }

    protected async Task<Guid> GetSeededCountryIdAsync(HttpClient client, string countryName = "USA")
    {
        var response = await client.GetAsync($"{GetAllCountriesUrl}?pageSize=1&name={countryName}");
        response.EnsureSuccessStatusCode();
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CountryResponse>>();
        var country = pagedList!.Items.FirstOrDefault(c => c.Name == countryName);
        return country?.Id ?? throw new InvalidOperationException($"Seeded country '{countryName}' not found.");
    }

    protected async Task<Guid> GetSeededCityIdAsync(HttpClient client, Guid countryId, string cityName)
    {
        var getAllUrl = $"{GetCitiesBaseUrl(countryId)}{CitiesEndpointsConstants.Paths.GetAll}?pageSize=1&name={cityName}";
        var response = await client.GetAsync(getAllUrl);
        response.EnsureSuccessStatusCode();
        var pagedList = await response.Content.ReadFromJsonAsync<PagedList<CityResponse>>();
        var city = pagedList!.Items.FirstOrDefault(c => c.Name == cityName);
        return city?.Id ?? throw new InvalidOperationException($"Seeded city '{cityName}' not found.");
    }

    protected string GetGetCountryByIdUrl(Guid id) => CountriesBaseUrl + CountriesEndpointsConstants.Paths.GetById.Replace("{id:guid}", id.ToString());
    protected string GetUpdateCountryUrl(Guid id) => CountriesBaseUrl + CountriesEndpointsConstants.Paths.Update.Replace("{id:guid}", id.ToString());
    protected string GetDeleteCountryUrl(Guid id) => CountriesBaseUrl + CountriesEndpointsConstants.Paths.Delete.Replace("{id:guid}", id.ToString());
    protected string GetCitiesBaseUrl(Guid countryId) => $"{BaseApi}{CitiesEndpointsConstants.Paths.Main.Replace("{countryId:guid}", countryId.ToString())}";
    protected string GetCreateCityUrl(Guid countryId) => $"{GetCitiesBaseUrl(countryId)}{CitiesEndpointsConstants.Paths.Create}";
    protected string GetAllCitiesUrl(Guid countryId) => $"{GetCitiesBaseUrl(countryId)}{CitiesEndpointsConstants.Paths.GetAll}";
    protected string GetCityByIdUrl(Guid countryId, Guid cityId) => $"{GetCitiesBaseUrl(countryId)}{CitiesEndpointsConstants.Paths.GetById.Replace("{id:guid}", cityId.ToString())}";
    protected string GetUpdateCityUrl(Guid countryId, Guid cityId) => $"{GetCitiesBaseUrl(countryId)}{CitiesEndpointsConstants.Paths.Update.Replace("{id:guid}", cityId.ToString())}";
    protected string GetDeleteCityUrl(Guid countryId, Guid cityId) => $"{GetCitiesBaseUrl(countryId)}{CitiesEndpointsConstants.Paths.Delete.Replace("{id:guid}", cityId.ToString())}";

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginRequest = new LoginRequest(username, password);
        var response = await client.PostAsJsonAsync(LoginUrl, loginRequest);

        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse!.Token);

        return client;
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;
    public virtual Task DisposeAsync() => Task.CompletedTask;

    public class NestedWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(ApplicationTestingEnvironmentsConstants.AcceptanceTest);

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlServer(_msSqlContainer.GetConnectionString());
                });
                services.AddScoped<DataSeeder>();
            });
        }
        public async Task InitializeAsync()
        {
            await _msSqlContainer.StartAsync();

            using var scope = Services.CreateScope();
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            var seeder = services.GetRequiredService<DataSeeder>();
            await seeder.SeedAsync();
        }

        public override async ValueTask DisposeAsync()
        {
            await _msSqlContainer.StopAsync();
            await base.DisposeAsync();
        }

        async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync().AsTask();
    }
}