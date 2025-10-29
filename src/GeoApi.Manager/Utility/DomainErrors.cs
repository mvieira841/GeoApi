using FluentResults;

namespace GeoApi.Manager.Utility;

public static class DomainErrors
{
    // Generic
    public static IError NotFound(string resourceName) =>
        new Error($"{resourceName} not found.")
            .WithMetadata("ErrorType", "NotFound");

    public static IError Conflict(string message) =>
        new Error(message)
            .WithMetadata("ErrorType", "Conflict");

    public static readonly IError CountryNotFound = NotFound("Country");
    public static readonly IError CityNotFound = NotFound("City");

    public static readonly IError CountryConflict = Conflict("Country with this name already exists.");
}