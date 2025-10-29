using GeoApi.Abstractions.Interfaces.Managers;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Cities;
using GeoApi.Abstractions.Responses.Cities;
using GeoApi.Host.ApiHelpers;
using GeoApi.Host.Endpoints.Constants;
using static GeoApi.Host.Endpoints.Constants.CitiesEndpointsConstants;

namespace GeoApi.Host.Endpoints.Map;

public static class CitiesEndpoints
{
    public static IEndpointRouteBuilder MapCitiesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup(Paths.Main)
            .WithTags(ResourceName)
            .AddEndpointFilterFactory(ValidationFilter.Factory);

        group.MapGet(Paths.GetAll, async (
                Guid countryId,
                [AsParameters] GetAllCitiesRequest request,
                ICityManager manager,
                CancellationToken ct) =>
        {
            var result = await manager.GetAllForCountryAsync(countryId, request, ct);
            return result.ToHttpResult();
        })
            .RequireAuthorization(AuthPoliciesConstants.RequireUserRolePolicy)
            .Produces<PagedList<CityResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithName(EndpointNames.GetAll)
            .WithOpenApi(operation =>
            {
                operation.Summary = SummaryDescriptions.GetAllByCountry;
                operation.Parameters = operation.Parameters.Select(x =>
                {
                    x.Name = char.ToLowerInvariant(x.Name[0]) + x.Name.Substring(1);
                    return x;
                }).ToList();

                var countryIdParam = operation.Parameters.FirstOrDefault(p => p.Name == "countryId");
                if (countryIdParam != null)
                    countryIdParam.Description = "The unique identifier of the parent country.";

                var nameParam = operation.Parameters.FirstOrDefault(p => p.Name == "name");
                if (nameParam != null)
                    nameParam.Description = "Optional. Filter cities by name (case-insensitive, partial match).";

                var latParam = operation.Parameters.FirstOrDefault(p => p.Name == "latitude");
                if (latParam != null)
                    latParam.Description = "Optional. Filter cities by an exact latitude.";

                var lonParam = operation.Parameters.FirstOrDefault(p => p.Name == "longitude");
                if (lonParam != null)
                    lonParam.Description = "Optional. Filter cities by an exact longitude.";

                var sortColumnParam = operation.Parameters.FirstOrDefault(p => p.Name == "sortColumn");
                if (sortColumnParam != null)
                    sortColumnParam.Description = "Optional. The column to sort by (e.g., 'Name', 'Latitude', 'Longitude'). Default is 'Name'.";

                var sortOrderParam = operation.Parameters.FirstOrDefault(p => p.Name == "sortOrder");
                if (sortOrderParam != null)
                    sortOrderParam.Description = "Optional. Sort direction ('ASC' or 'DESC'). Default is 'ASC'.";

                var pageParam = operation.Parameters.FirstOrDefault(p => p.Name == "page");
                if (pageParam != null)
                    pageParam.Description = "Optional. The page number for pagination. Default is 1.";

                var pageSizeParam = operation.Parameters.FirstOrDefault(p => p.Name == "pageSize");
                if (pageSizeParam != null)
                    pageSizeParam.Description = "Optional. The number of items per page. Default is 10.";

                return operation;
            });

        group.MapGet(Paths.GetById, async (
                Guid countryId,
                Guid id,
                ICityManager manager,
                CancellationToken ct) =>
        {
            var result = await manager.GetByIdAsync(id, ct);
            return result.ToHttpResult();
        })
            .RequireAuthorization(AuthPoliciesConstants.RequireUserRolePolicy)
            .Produces<CityResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithName(EndpointNames.GetById)
            .WithOpenApi(operation =>
            {
                operation.Summary = SummaryDescriptions.GetById;

                var countryIdParam = operation.Parameters.FirstOrDefault(p => p.Name == "countryId");
                if (countryIdParam != null)
                    countryIdParam.Description = "The unique identifier of the parent country.";

                var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
                if (idParam != null)
                    idParam.Description = "The unique identifier of the city.";

                return operation;
            });

        group.MapPost(Paths.Create, async (
                Guid countryId,
                CreateCityRequest request,
                ICityManager manager,
                CancellationToken ct) =>
        {
            var result = await manager.CreateAsync(countryId, request, ct);

            return result.IsSuccess
                ? Results.CreatedAtRoute(
                    EndpointNames.GetById,
                    new { countryId, id = result.Value.Id },
                    result.Value)
                : result.ToHttpResult();
        })
            .RequireAuthorization(AuthPoliciesConstants.RequireAdminRolePolicy)
            .Produces<CityResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithName(EndpointNames.Create)
            .WithOpenApi(operation =>
            {
                operation.Summary = SummaryDescriptions.Create;

                var countryIdParam = operation.Parameters.FirstOrDefault(p => p.Name == "countryId");
                if (countryIdParam != null)
                    countryIdParam.Description = "The unique identifier of the parent country.";

                return operation;
            });

        group.MapPut(Paths.Update, async (
                Guid countryId,
                Guid id,
                UpdateCityRequest request,
                ICityManager manager,
                CancellationToken ct) =>
        {
            var result = await manager.UpdateAsync(id, request, ct);
            return result.ToHttpResult();
        })
            .RequireAuthorization(AuthPoliciesConstants.RequireAdminRolePolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithName(EndpointNames.Update)
            .WithOpenApi(operation =>
            {
                operation.Summary = SummaryDescriptions.Update;

                var countryIdParam = operation.Parameters.FirstOrDefault(p => p.Name == "countryId");
                if (countryIdParam != null)
                    countryIdParam.Description = "The unique identifier of the parent country.";

                var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
                if (idParam != null)
                    idParam.Description = "The unique identifier of the city to update.";

                return operation;
            });

        group.MapDelete(Paths.Delete, async (
                Guid countryId,
                Guid id,
                ICityManager manager,
                CancellationToken ct) =>
        {
            var result = await manager.DeleteAsync(id, ct);
            return result.ToHttpResult();
        })
            .RequireAuthorization(AuthPoliciesConstants.RequireAdminRolePolicy)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithName(EndpointNames.Delete)
            .WithOpenApi(operation =>
            {
                operation.Summary = SummaryDescriptions.Delete;

                var countryIdParam = operation.Parameters.FirstOrDefault(p => p.Name == "countryId");
                if (countryIdParam != null)
                    countryIdParam.Description = "The unique identifier of the parent country.";

                var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
                if (idParam != null)
                    idParam.Description = "The unique identifier of the city to delete.";

                return operation;
            });

        return app;
    }
}