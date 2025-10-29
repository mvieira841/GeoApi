using GeoApi.Host.Endpoints.Map;

namespace GeoApi.Host.ApiHelpers;

public static class EndpointExtensions
{
    public static IApplicationBuilder MapApiEndpoints(this WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(ApiVersionSet.CurrentVersion)
            .ReportApiVersions()
            .Build();

        var currentVersion = app.MapGroup("/api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet);

        // Map all endpoint groups
        currentVersion.MapAuthEndpoints();
        currentVersion.MapCountriesEndpoints();
        currentVersion.MapCitiesEndpoints();

        return app;
    }
}