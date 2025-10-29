namespace GeoApi.Abstractions.Requests.Auth;

/// <summary>
/// Request model for user login.
/// </summary>
/// <param name="UserName">The user's username.</param>
/// <param name="Password">The user's password.</param>
public record LoginRequest(string UserName, string Password);