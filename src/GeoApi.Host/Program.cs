using GeoApi.Access;
using GeoApi.Access.Persistence;
using GeoApi.Access.Persistence.Context;
using GeoApi.Host;
using GeoApi.Host.ApiHelpers;
using GeoApi.Manager;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Configure Dependency Injection ---
builder.AddHost();
builder.Services.AddManager();
builder.Services.AddAccess(builder.Configuration);

// --- Build App ---
var app = builder.Build();

// exception handler registration
app.UseExceptionHandler();

// Enable Serilog Request Logging middleware
app.UseSerilogRequestLogging();

// Enable HTTP Logging middleware
app.UseHttpLogging();

// Enable middleware for serving generated Swagger JSON and Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint($"/swagger/{ApiVersionSet.CurrentVersionString}/swagger.json", $"GeoAPI {ApiVersionSet.CurrentVersionString}");
});

if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Log.Information("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied.");

        var dataSeeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        Log.Information("Seeding database...");
        await dataSeeder.SeedAsync();
        Log.Information("Database seeding completed.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred during development startup (migrations/seeding).");
    }
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map Minimal API Endpoints
app.MapApiEndpoints();

// --- Run App ---
try
{
    Log.Information("Starting GeoApi.Host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "GeoApi.Host terminated unexpectedly");
}
finally
{
    // Ensure Serilog is properly disposed on application shutdown
    Log.CloseAndFlush();
}

// Marker for WebApplicationFactory in tests
public partial class Program { }