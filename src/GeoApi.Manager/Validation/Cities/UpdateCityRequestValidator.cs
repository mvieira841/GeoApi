using FluentValidation;
using GeoApi.Abstractions.Requests.Cities;

namespace GeoApi.Manager.Validation.Cities;

public class UpdateCityRequestValidator : AbstractValidator<UpdateCityRequest>
{
    public UpdateCityRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}