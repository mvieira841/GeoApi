using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace GeoApi.Host.ApiHelpers;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : HandleFailedResult(result);
    }

    public static IResult ToHttpResult(this Result result)
    {
        return result.IsSuccess
            ? Results.NoContent()
            : HandleFailedResult(result);
    }

    private static IResult HandleFailedResult(IResultBase result)
    {
        if (result.Errors.Any(e => e.Metadata.ContainsKey("PropertyName")))
        {
            var validationErrors = result.Errors
                .Where(e => e.Metadata.ContainsKey("PropertyName")) // Be safe
                .GroupBy(e => (string)e.Metadata["PropertyName"])
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.Message).ToArray()
                );

            return Results.ValidationProblem(validationErrors);
        }

        var firstError = result.Errors.FirstOrDefault(e => e.Metadata.ContainsValue("NotFound"))
                        ?? result.Errors.FirstOrDefault(e => e.Metadata.ContainsValue("Unauthorized"))
                        ?? result.Errors.FirstOrDefault(e => e.Metadata.ContainsValue("Conflict"))
                        ?? result.Errors.FirstOrDefault(); // Fallback to the first error

        if (firstError is null)
        {
            return Results.Problem(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            });
        }

        (int statusCode, string title) = GetErrorDetails(firstError);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = firstError.Message,
            Extensions =
            {
                ["errors"] = result.Errors.Select(e => e.Message).ToList()
            }
        };

        return Results.Problem(problemDetails);
    }

    private static (int StatusCode, string Title) GetErrorDetails(IError error)
    {
        string errorType = "Failure";
        if (error.Metadata.TryGetValue("ErrorType", out var typeValue))
        {
            errorType = typeValue as string ?? "Failure";
        }

        return errorType switch
        {
            "NotFound" => (StatusCodes.Status404NotFound, "Resource Not Found"),
            "Unauthorized" => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            "Conflict" => (StatusCodes.Status409Conflict, "Conflict"),

            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };
    }
}