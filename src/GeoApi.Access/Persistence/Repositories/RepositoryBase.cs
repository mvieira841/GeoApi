using GeoApi.Abstractions.Pagination;
using GeoApi.Access.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace GeoApi.Access.Persistence.Repositories;

public abstract class RepositoryBase<TEntity>(ApplicationDbContext dbContext)
    where TEntity : BaseEntity
{
    protected readonly ApplicationDbContext DbContext = dbContext;

    protected static async Task<PagedList<TEntity>> CreatePagedListAsync(
        IQueryable<TEntity> query,
        PagedRequest request,
        CancellationToken ct)
    {
        var sortOrder = "ASC".Equals(request.SortOrderValue, StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : "DESC";
        query = query.OrderBy($"{request.SortColumnValue} {sortOrder}");
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSizeValue)
            .Take(request.PageSizeValue)
            .ToListAsync(ct);

        return new PagedList<TEntity>(items, request.PageNumber, request.PageSizeValue, totalCount);
    }
}