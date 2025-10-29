using Bogus;
using GeoApi.Abstractions.Entities;
using GeoApi.Abstractions.Enums;
using GeoApi.Access.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GeoApi.Access.Persistence;

public class DataSeeder(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ILogger<DataSeeder> logger)
{
    public const string AdminUserName = "admin";
    public const string AdminEmail = "admin@geoapi.com";
    public const string AdminPassword = "Admin123!";

    public const string UserUserName = "user";
    public const string UserEmail = "user@geoapi.com";
    public const string UserPassword = "User123!";

    // A known name for a bogus country to check if seeding already ran
    private const string BogusCountryCheckName = "Bogus_Country_0";

    public async Task SeedAsync(bool includeCountriesAndCities = true)
    {
        try
        {
            await SeedRolesAsync();
            await SeedAdminUserAsync();
            await SeedUserAsync();
            if (includeCountriesAndCities)
            {
                await SeedCountriesAndCitiesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedRolesAsync()
    {
        if (!await roleManager.RoleExistsAsync(UserRoleConstants.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(UserRoleConstants.Admin));
            logger.LogInformation("'{Role}' role created.", UserRoleConstants.Admin);
        }

        if (!await roleManager.RoleExistsAsync(UserRoleConstants.User))
        {
            await roleManager.CreateAsync(new IdentityRole(UserRoleConstants.User));
            logger.LogInformation("'{Role}' role created.", UserRoleConstants.User);
        }
    }
    public async Task SeedAdminUserAsync()
    {
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = AdminUserName,
                Email = AdminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };

            await AddUserAsync(adminUser, AdminPassword, [UserRoleConstants.Admin]);
        }
    }

    public async Task SeedUserAsync()
    {
        if (await userManager.FindByEmailAsync(UserEmail) is null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = UserUserName,
                Email = UserEmail,
                FirstName = "UserFirstName",
                LastName = "UserLastName",
                EmailConfirmed = true
            };

            await AddUserAsync(adminUser, UserPassword, new List<string> { UserRoleConstants.User });
        }
    }

    public async Task AddUserAsync(ApplicationUser applicationUser, string password, List<string> roles)
    {
        if (await userManager.FindByEmailAsync(applicationUser.Email!) is null)
        {
            var result = await userManager.CreateAsync(applicationUser, password);
            if (result.Succeeded)
            {
                foreach (var role in roles)
                {
                    await userManager.AddToRoleAsync(applicationUser, role);
                }
                logger.LogInformation("User {UserName} created and assigned to roles: {Roles}", 
                    applicationUser.UserName, string.Join(", ", roles));
            }
            else
            {
                logger.LogWarning("Could not create user {UserName}: {Errors}", applicationUser.UserName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedCountriesAndCitiesAsync()
    {
        if (await context.Countries.AnyAsync(c => c.IsoCode == "USA"))
        {
            logger.LogInformation("Real country and City data already exists. Skipping.");
            return;
        }

        logger.LogInformation("Seeding real Countries and Cities...");
        var countries = GetCountries();

        await context.Countries.AddRangeAsync(countries);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} real countries and their cities.", countries.Count);
    }
    private async Task SeedBogusCountriesAndCitiesAsync(int countryCount, int citiesPerCountry)
    {
        if (await context.Countries.AnyAsync(c => c.Name == BogusCountryCheckName))
        {
            logger.LogInformation("Bogus country and city data already exists. Skipping.");
            return;
        }

        logger.LogInformation("Seeding {CountryCount} bogus countries, each with {CityCount} bogus cities...", countryCount, citiesPerCountry);

        // Set a seed for reproducible fake data
        Randomizer.Seed = new Random(456);

        var cityFaker = new Faker<City>()
            .RuleFor(c => c.Name, f => f.Address.City())
            .RuleFor(c => c.Latitude, f => Convert.ToDecimal(f.Address.Latitude()))
            .RuleFor(c => c.Longitude, f => Convert.ToDecimal(f.Address.Longitude()));

        var countryFaker = new Faker<Country>()
            .RuleFor(c => c.Name, (f, c) => $"Bogus_Country_{f.IndexFaker}")
            .RuleFor(c => c.IsoCode, f => f.Random.String2(3).ToUpper())
            .RuleFor(c => c.Cities, (f, c) =>
            {
                // Generate a list of cities for this country
                // We must .ToList() to execute the generation
                return cityFaker.Generate(citiesPerCountry).ToList();
            });

        try
        {
            var bogusCountries = countryFaker.Generate(countryCount);

            // Bogus can generate duplicate IsoCodes or City names within a country,
            // which our database constraints will reject. We must manually unique-ify them.
            var IsoCodes = new HashSet<string>();
            var cityNames = new HashSet<string>();

            foreach (var country in bogusCountries)
            {
                // Ensure unique IsoCode
                while (!IsoCodes.Add(country.IsoCode))
                {
                    country.IsoCode = Randomizer.Seed.Next(100, 999).ToString();
                }

                // Ensure unique city names *within* this country
                cityNames.Clear();
                foreach (var city in country.Cities)
                {
                    string originalName = city.Name;
                    int suffix = 1;
                    while (!cityNames.Add(city.Name))
                    {
                        city.Name = $"{originalName}_{suffix++}";
                    }
                }
            }

            await context.Countries.AddRangeAsync(bogusCountries);
            await context.SaveChangesAsync();

            logger.LogInformation("Successfully seeded {CountryCount} bogus countries and {CityCount} total bogus cities.",
                countryCount, countryCount * citiesPerCountry);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding bogus data. This might be due to unique constraint violations if data was partially seeded.");
        }
    }

    private async Task SeedUsersAsync(int count)
    {
        if (await userManager.Users.CountAsync() > 100) // Check if users (minus admin) exist
        {
            logger.LogInformation("User data already exists. Skipping bulk user seed.");
            return;
        }

        logger.LogInformation("Seeding {Count} users...", count);

        Randomizer.Seed = new Random(123);

        var userFaker = new Faker<ApplicationUser>()
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.UserName, (f, u) => f.Internet.UserName(u.FirstName, u.LastName))
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
            .RuleFor(u => u.EmailConfirmed, true);

        int batchSize = 1000;
        int createdCount = 0;

        for (int i = 0; i < count / batchSize; i++)
        {
            // Load existing users ONCE per batch to avoid N+1 queries
            var existingUsernames = await userManager.Users.Select(u => u.UserName).ToHashSetAsync();
            var existingEmails = await userManager.Users.Select(u => u.Email).ToHashSetAsync();

            var users = new List<ApplicationUser>();
            for (int j = 0; j < batchSize; j++)
            {
                users.Add(userFaker.Generate());
            }

            foreach (var user in users)
            {
                string originalEmail = user.Email!;
                string originalUserName = user.UserName!;
                int suffix = 1;

                // Check against the IN-MEMORY set
                while (existingEmails.Contains(user.Email!))
                {
                    user.Email = $"{suffix++}_{originalEmail}";
                }
                existingEmails.Add(user.Email!); // Add to set for this batch

                suffix = 1;
                // Check against the IN-MEMORY set
                while (existingUsernames.Contains(user.UserName!))
                {
                    user.UserName = $"{originalUserName}_{suffix++}";
                }
                existingUsernames.Add(user.UserName!); // Add to set for this batch

                var result = await userManager.CreateAsync(user, "User123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, UserRoleConstants.User);
                    createdCount++;
                }
                else
                {
                    logger.LogWarning("Failed to create user {Email}: {Errors}", user.Email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            logger.LogInformation("Created batch {BatchNumber} of users. Total so far: {Total}", i + 1, createdCount);
        }

        logger.LogInformation("Successfully created {Count} users and assigned to '{Role}' role.", createdCount, UserRoleConstants.User);
    }

    // Data Source
    private static List<Country> GetCountries()
    {
        return
        [
            new Country
            {
                Name = "USA", IsoCode = "USA", Cities =
                [
                    new() { Name = "New York", Latitude = 40.7128m, Longitude = -74.0060m },
                    new() { Name = "Los Angeles", Latitude = 34.0522m, Longitude = -118.2437m },
                    new() { Name = "Chicago", Latitude = 41.8781m, Longitude = -87.6298m },
                    new() { Name = "Houston", Latitude = 29.7604m, Longitude = -95.3698m },
                    new() { Name = "Phoenix", Latitude = 33.4484m, Longitude = -112.0740m },
                ]
            },
            new Country
            {
                Name = "Canada", IsoCode = "CAN", Cities =
                [
                    new() { Name = "Toronto", Latitude = 43.6532m, Longitude = -79.3832m },
                    new() { Name = "Vancouver", Latitude = 49.2827m, Longitude = -123.1207m },
                    new() { Name = "Montreal", Latitude = 45.5017m, Longitude = -73.5673m },
                ]
            },
            new Country
            {
                Name = "Brazil", IsoCode = "BRA", Cities =
                [
                    new() { Name = "São Paulo", Latitude = -23.5505m, Longitude = -46.6333m },
                    new() { Name = "Rio de Janeiro", Latitude = -22.9068m, Longitude = -43.1729m },
                    new() { Name = "Brasília", Latitude = -15.8267m, Longitude = -47.9218m },
                ]
            },
            new Country
            {
                Name = "Germany", IsoCode = "DEU", Cities =
                [
                    new() { Name = "Berlin", Latitude = 52.5200m, Longitude = 13.4050m },
                    new() { Name = "Hamburg", Latitude = 53.5511m, Longitude = 9.9937m },
                    new() { Name = "Munich", Latitude = 48.1351m, Longitude = 11.5820m },
                ]
            },
            new Country
            {
                Name = "France", IsoCode = "FRA", Cities =
                [
                    new() { Name = "Paris", Latitude = 48.8566m, Longitude = 2.3522m },
                    new() { Name = "Marseille", Latitude = 43.2965m, Longitude = 5.3698m },
                ]
            },
            new Country
            {
                Name = "Japan", IsoCode = "JPN", Cities =
                [
                    new() { Name = "Tokyo", Latitude = 35.6895m, Longitude = 139.6917m },
                    new() { Name = "Osaka", Latitude = 34.6937m, Longitude = 135.5023m },
                    new() { Name = "Kyoto", Latitude = 35.0116m, Longitude = 135.7681m },
                ]
            },
            new Country
            {
                Name = "Australia", IsoCode = "AUS", Cities =
                [
                    new() { Name = "Sydney", Latitude = -33.8688m, Longitude = 151.2093m },
                    new() { Name = "Melbourne", Latitude = -37.8136m, Longitude = 144.9631m },
                ]
            },
            new Country
            {
                Name = "India", IsoCode = "IND", Cities =
                [
                    new() { Name = "Delhi", Latitude = 28.7041m, Longitude = 77.1025m },
                    new() { Name = "Mumbai", Latitude = 19.0760m, Longitude = 72.8777m },
                    new() { Name = "Bangalore", Latitude = 12.9716m, Longitude = 77.5946m },
                ]
            },
            new Country
            {
                Name = "United Kingdom", IsoCode = "GBR", Cities =
                [
                    new() { Name = "London", Latitude = 51.5074m, Longitude = -0.1278m },
                    new() { Name = "Manchester", Latitude = 53.4808m, Longitude = -2.2426m },
                ]
            },
            new Country
            {
                Name = "South Africa", IsoCode = "ZAF", Cities =
                [
                    new() { Name = "Johannesburg", Latitude = -26.2041m, Longitude = 28.0473m },
                    new() { Name = "Cape Town", Latitude = -33.9249m, Longitude = 18.4241m },
                ]
            }
        ];
    }
}

