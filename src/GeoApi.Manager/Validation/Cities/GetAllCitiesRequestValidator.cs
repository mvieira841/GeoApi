using FluentValidation;
using GeoApi.Abstractions.Requests.Cities;
using GeoApi.Manager.Validation.Common;

namespace GeoApi.Manager.Validation.Cities;

public class GetAllCitiesRequestValidator : PagedRequestValidator<GetAllCitiesRequest>
{
    private static List<string> validSortColumns = new()
    {
        "Id",
        "Name",
        "Country",
        "Latitude",
        "Longitude"
    };
    public GetAllCitiesRequestValidator() : base(validSortColumns)
    {
        RuleFor(x => x.Name).MaximumLength(100);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude.HasValue);
    }
}