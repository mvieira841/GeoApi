using FluentValidation;
using GeoApi.Abstractions.Requests.Auth;

namespace GeoApi.Manager.Validation.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain one number.");
    }
}