using GeoApi.Abstractions.Interfaces.Managers;
using GeoApi.Abstractions.Pagination;
using GeoApi.Abstractions.Requests.Countries;
using GeoApi.Abstractions.Responses.Countries;
using GeoApi.Host.ApiHelpers;
using GeoApi.Host.Endpoints.Constants;
using static GeoApi.Host.Endpoints.Constants.CountriesEndpointsConstants;

namespace GeoApi.Host.Endpoints.Map;

public static class CountriesEndpoints
{
    public static IEndpointRouteBuilder MapCountriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup(Paths.Main)
            .WithTags(ResourceName)
            .AddEndpointFilterFactory(ValidationFilter.Factory);

        group.MapGet(Paths.GetAll, async (
                [AsParameters] GetAllCountriesRequest request,
                ICountryManager manager,
                CancellationToken ct) =>
        {
            var result = await manager.GetAllAsync(request, ct);
            return result.ToHttpResult();
        })
            .RequireAuthorization(AuthPoliciesConstants.RequireUserRolePolicy)
            .Produces<PagedList<CountryResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError) 
            .WithName(EndpointNames.GetAll)
            .WithOpenApi(operation =>
            {
                operation.Summary = SummaryDescriptions.GetAll;
                operation.Parameters = operation.Parameters.Select(x =>
                {
                    x.Name = char.ToLowerInvariant(x.Name[0]) + x.Name.Substring(1);
                    return x;
                }).ToList();

                var nameParam = operation.Parameters.FirstOrDefault(p => p.Name == "name");
                if (nameParam != null) 
                    nameParam.Description = "Optional. Filter countries by name (case-insensitive, partial match).";
                
                var isoCodeParam = operation.Parameters.FirstOrDefault(p => p.Name == "isoCode");
                if (isoCodeParam != null) 
                    isoCodeParam.Description = "Optional. Filter countries by the exact 3-letter ISO code.";

                var sortColumnParam = operation.Parameters.FirstOrDefault(p => p.Name == "sortColumn");
                if (sortColumnParam != null) 
                    sortColumnParam.Description = "Optional. The column to sort by (e.g., 'Name', 'IsoCode'). Default is 'Name'.";
                
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
                Guid id,
                ICountryManager manager,
                CancellationToken ct) =>
        {
            var result = await manager.GetByIdAsync(id, ct);
            return result.ToHttpResult();
        })
            .RequireAuthorization(AuthPoliciesConstants.RequireUserRolePolicy)
            .Produces<CountryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound) 
            .ProducesProblem(StatusCodes.Status500InternalServerError) 
            .WithName(EndpointNames.GetById)
            .WithOpenApi(operation =>
            {
                operation.Summary = SummaryDescriptions.GetById;
                
                var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
                if (idParam != null) 
                    idParam.Description = "The unique identifier of the country.";
                
                return operation;
            });

        group.MapPost(Paths.Create, async (
                CreateCountryRequest request,
                ICountryManager manager,
                CancellationToken ct) =>
        {
            var result = await manager.CreateAsync(request, ct);

            return result.IsSuccess
                ? Results.CreatedAtRoute(
                    EndpointNames.GetById,
                    new { id = result.Value.Id },
                    result.Value)
                : result.ToHttpResult();
        })
            .RequireAuthorization(AuthPoliciesConstants.RequireAdminRolePolicy)
            .Produces<CountryResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest) 
            .ProducesProblem(StatusCodes.Status409Conflict) 
            .ProducesProblem(StatusCodes.Status500InternalServerError) 
            .WithName(EndpointNames.Create)
            .WithOpenApi(operation =>
            {
                operation.Summary = SummaryDescriptions.Create;
                // 'request' is request body
                return operation;
            });

        group.MapPut(Paths.Update, async (
                Guid id,
                UpdateCountryRequest request,
                ICountryManager manager,
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
                
                var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
                if (idParam != null) 
                    idParam.Description = "The unique identifier of the country to update.";
                
                return operation;
            });

        group.MapDelete(Paths.Delete, async (
                Guid id,
                ICountryManager manager,
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
                
                var idParam = operation.Parameters.FirstOrDefault(p => p.Name == "id");
                if (idParam != null) 
                    idParam.Description = "The unique identifier of the country to delete.";
                
                return operation;
            });

        return app;
    }
}