using FluentValidation;

namespace GeoApi.Host.ApiHelpers;

public class ValidationFilter : IEndpointFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var arg in context.Arguments)
        {
            if (arg is null) continue;

            var argType = arg.GetType();

            var validatorType = typeof(IValidator<>).MakeGenericType(argType);

            using (var scope = _serviceProvider.CreateScope())
            {
                var validator = scope.ServiceProvider.GetService(validatorType) as IValidator;

                if (validator is not null)
                {
                    var validationContext = new ValidationContext<object>(arg);
                    var validationResult = await validator.ValidateAsync(validationContext);

                    if (!validationResult.IsValid)
                    {
                        return Results.ValidationProblem(validationResult.ToDictionary());
                    }
                }
            }
        }

        return await next(context);
    }

    public static EndpointFilterDelegate Factory(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        return async (invocationContext) =>
        {
            var serviceProvider = invocationContext.HttpContext.RequestServices;
            var validationFilter = new ValidationFilter(serviceProvider);
            return await validationFilter.InvokeAsync(invocationContext, next);
        };
    }
}