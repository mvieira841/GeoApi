using FluentValidation;
using GeoApi.Abstractions.Requests.Countries;
using GeoApi.Manager.Validation.Common;

namespace GeoApi.Manager.Validation.Countries;

public class GetAllCountriesRequestValidator : PagedRequestValidator<GetAllCountriesRequest>
{
    private static List<string> validSortColumns = new()
    {
        "Id",
        "Name",
        "IsoCode",
    };
    public GetAllCountriesRequestValidator() : base(validSortColumns)
    {
        RuleFor(x => x.Name)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.IsoCode)
            .MaximumLength(3)
            .When(x => !string.IsNullOrEmpty(x.IsoCode));
    }
}