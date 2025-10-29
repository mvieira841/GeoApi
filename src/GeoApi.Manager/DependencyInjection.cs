using System.Reflection;
using FluentValidation;
using GeoApi.Abstractions.Interfaces.Managers;
using GeoApi.Manager.Managers;
using Microsoft.Extensions.DependencyInjection;

namespace GeoApi.Manager;

public static class DependencyInjection
{
    public static IServiceCollection AddManager(this IServiceCollection services)
    {
        // Add Managers
        services.AddScoped<ICountryManager, CountryManager>();
        services.AddScoped<ICityManager, CityManager>();
        services.AddScoped<IAuthManager, AuthManager>();

        // Add Validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}