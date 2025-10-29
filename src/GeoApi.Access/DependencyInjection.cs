using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Access.Persistence;
using GeoApi.Access.Persistence.Context;
using GeoApi.Access.Persistence.Repositories;
using GeoApi.Access.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GeoApi.Access;

public static class DependencyInjection
{
    public static IServiceCollection AddAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Add Identity
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
        })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Add Data Seeder
        services.AddScoped<DataSeeder>();

        // Add Identity's SignInManager
        services.AddScoped<SignInManager<ApplicationUser>>();

        // Add Repositories
        services.AddScoped<ICountryRepository, CountryRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Add Services
        services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();

        return services;
    }
}