using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GeoApi.Host.ApiHelpers;

internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception, "Unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails();

        switch (exception)
        {
            case BadHttpRequestException badRequestException:
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Bad Request";
                problemDetails.Detail = badRequestException.Message;
                break;
            default:
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Internal Server Error";
                break;
        }

        var showProblemDetails = environment.IsDevelopment()
                                 || environment.IsEnvironment(ApplicationTestingEnvironmentsConstants.AcceptanceTest)
                                 || environment.IsEnvironment(ApplicationTestingEnvironmentsConstants.IntegrationTest);

        if (showProblemDetails)
        {
            problemDetails.Title = exception.GetType().Name;
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }
        else
        {
            if (problemDetails.Status == StatusCodes.Status500InternalServerError)
            {
                problemDetails.Detail = "An unexpected error occurred. Please try again later.";
            }
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}