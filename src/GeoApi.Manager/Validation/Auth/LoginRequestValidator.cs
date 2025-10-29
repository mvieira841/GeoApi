using FluentValidation;
using GeoApi.Abstractions.Requests.Auth;

namespace GeoApi.Manager.Validation.Auth;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}