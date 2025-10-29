using FluentValidation;
using GeoApi.Abstractions.Requests.Countries;

namespace GeoApi.Manager.Validation.Countries;

public class UpdateCountryRequestValidator : AbstractValidator<UpdateCountryRequest>
{
    public UpdateCountryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.IsoCode).NotEmpty().Length(3);
    }
}