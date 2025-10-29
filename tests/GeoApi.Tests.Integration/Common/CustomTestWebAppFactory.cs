using GeoApi.Access.Persistence;
using GeoApi.Access.Persistence.Context;
using GeoApi.Host.ApiHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;

namespace GeoApi.Tests.Integration.Common;

public abstract class CustomTestWebAppFactory : IAsyncLifetime, IClassFixture<CustomTestWebAppFactory.NestedWebAppFactory>
{
    protected readonly IServiceScope Scope;
    protected readonly ApplicationDbContext DbContext;
    protected readonly IServiceProvider ServiceProvider;
    private IDbContextTransaction? _transaction;
    protected CustomTestWebAppFactory(NestedWebAppFactory factory)
    {
        Scope = factory.Services.CreateScope();
        ServiceProvider = Scope.ServiceProvider;
        DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public async Task InitializeAsync()
    {
        _transaction = await DbContext.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }
        Scope.Dispose();
    }

    protected async Task SeedAsync<TEntity>(params TEntity[] entities) where TEntity : class
    {
        await DbContext.Set<TEntity>().AddRangeAsync(entities);
        await DbContext.SaveChangesAsync();
    }

    public class NestedWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Strong!Password123")
            .Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(ApplicationTestingEnvironmentsConstants.IntegrationTest);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseSqlServer(_dbContainer.GetConnectionString());
                });
            });
        }

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            using var scope = Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            await seeder.SeedAsync(includeCountriesAndCities: false);
        }

        public new async Task DisposeAsync()
        {
            await _dbContainer.StopAsync();
        }
    }
}