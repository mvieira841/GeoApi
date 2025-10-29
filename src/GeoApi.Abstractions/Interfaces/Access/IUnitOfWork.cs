namespace GeoApi.Abstractions.Interfaces.Access;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}