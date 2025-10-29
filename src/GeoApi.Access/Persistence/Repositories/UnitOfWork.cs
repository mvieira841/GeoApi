using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Access.Persistence.Context;

namespace GeoApi.Access.Persistence.Repositories;

internal sealed class UnitOfWork(ApplicationDbContext dbContext): IUnitOfWork
{

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return dbContext.SaveChangesAsync(ct);
    }
}