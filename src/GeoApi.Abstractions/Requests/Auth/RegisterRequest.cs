namespace GeoApi.Abstractions.Requests.Auth;

/// <summary>
/// Request model for new user registration.
/// </summary>
/// <param name="FirstName">The user's first name.</param>
/// <param name="LastName">The user's last name.</param>
/// <param name="UserName">The user's unique username.</param>
/// <param name="Email">The user's unique email address.</param>
/// <param name="Password">The user's password. (Must meet complexity requirements).</param>
public record RegisterRequest(
    string FirstName,
    string LastName,
    string UserName,
    string Email,
    string Password);