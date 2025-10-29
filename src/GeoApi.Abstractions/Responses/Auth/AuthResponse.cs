namespace GeoApi.Abstractions.Responses.Auth;

/// <summary>
/// Represents the successful authentication response.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="UserName">The user's username.</param>
/// <param name="Token">The JWT (JSON Web Token) for authenticating subsequent requests.</param>
public record AuthResponse(string Email, string UserName, string Token);