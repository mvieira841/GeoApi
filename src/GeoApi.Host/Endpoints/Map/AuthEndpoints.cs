using GeoApi.Abstractions.Interfaces.Managers;
using GeoApi.Abstractions.Requests.Auth;
using GeoApi.Abstractions.Responses.Auth;
using GeoApi.Host.ApiHelpers;
using static GeoApi.Host.Endpoints.Constants.AuthEndpointsConstants;

namespace GeoApi.Host.Endpoints.Map;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Paths.Main)
            .WithTags(ResourceName)
            .AllowAnonymous()
            .AddEndpointFilterFactory(ValidationFilter.Factory);

        group.MapPost(Paths.Register, async (
                RegisterRequest request,
                IAuthManager authManager,
                CancellationToken ct) =>
        {
            var result = await authManager.RegisterUserAsync(request, ct);
            return result.ToHttpResult();
        })
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithName(EndpointNames.Register)
            .WithOpenApi(operation =>
            {
                operation.Summary = SummaryDescriptions.Register;
                return operation;
            });

        group.MapPost(Paths.Login, async (
                LoginRequest request,
                IAuthManager authManager,
                CancellationToken ct) =>
        {
            var result = await authManager.LoginUserAsync(request, ct);
            return result.ToHttpResult();
        })
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithName(EndpointNames.Login)
            .WithOpenApi(operation =>
            {
                operation.Summary = SummaryDescriptions.Login;
                return operation;
            });

        return app;
    }
}