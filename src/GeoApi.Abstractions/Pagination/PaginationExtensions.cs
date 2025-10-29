namespace GeoApi.Abstractions.Pagination;

public static class PaginationExtensions
{
    public static PagedList<TResponse> ToResponse<TEntity, TResponse>(
        this PagedList<TEntity> pagedList,
        Func<TEntity, TResponse> mapper)
    {
        var responseItems = pagedList.Items.Select(mapper).ToList();
        return new PagedList<TResponse>(
            responseItems,
            pagedList.Page,
            pagedList.PageSize,
            pagedList.TotalCount);
    }
}