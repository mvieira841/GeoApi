using FluentValidation;

namespace GeoApi.Manager.Validation.Common;

public class PagedRequestValidator<T> : AbstractValidator<T> where T : PagedRequest
{
    private const int MinPage = 1;
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;

    private static readonly HashSet<string> ValidSortOrders = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "ASC",
        "DESC"
    };

    private readonly HashSet<string> _validSortNames;

    public PagedRequestValidator(List<string> validSortNames)
    {
        _validSortNames = validSortNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(MinPage)
            .When(x => x.Page.HasValue)
            .WithMessage($"Page must be greater than or equal to {MinPage}.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(MinPageSize, MaxPageSize)
            .When(x => x.PageSize.HasValue)
            .WithMessage($"PageSize must be between {MinPageSize} and {MaxPageSize}.");

        RuleFor(x => x.SortOrder)
            .Must(BeValidSortOrder)
            .When(x => !string.IsNullOrEmpty(x.SortOrder))
            .WithMessage("SortOrder must be 'ASC' or 'DESC'.");

        RuleFor(x => x.SortColumn)
            .Must(BeValidSortColumn)
            .When(x => !string.IsNullOrEmpty(x.SortColumn))
            .WithMessage($"SortColumn must be one of: {string.Join(", ", validSortNames)}");
    }

    private bool BeValidSortOrder(string? sortOrder)
    {
        if (string.IsNullOrEmpty(sortOrder)) return true;
        return ValidSortOrders.Contains(sortOrder);
    }

    private bool BeValidSortColumn(string? sortColumn)
    {
        // This method is only called if sortColumn is not null or empty (due to the .When())
        // but we check again for safety.
        if (string.IsNullOrEmpty(sortColumn)) return true;

        // Check if the provided column name is in our allowed list
        return _validSortNames.Contains(sortColumn);
    }
}