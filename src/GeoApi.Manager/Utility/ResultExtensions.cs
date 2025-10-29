using FluentResults;
using FluentValidation.Results;

namespace GeoApi.Manager.Utility;

public static class ResultExtensions
{
    public static List<IError> ToFluentErrors(this IEnumerable<ValidationFailure> validationErrors)
    {
        return validationErrors
            .Select(e => new Error(e.ErrorMessage).WithMetadata("PropertyName", e.PropertyName))
            .Cast<IError>()
            .ToList();
    }

    public static Result ToFluentErrorsResult(this ValidationResult validationResult)
    {
        var errors = validationResult.Errors.ToFluentErrors();
        return Result.Fail(errors);
    }
}