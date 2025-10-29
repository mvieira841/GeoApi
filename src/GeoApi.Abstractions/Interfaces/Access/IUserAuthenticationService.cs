using FluentResults;
using GeoApi.Abstractions.Requests.Auth;
using GeoApi.Abstractions.Responses.Auth;

namespace GeoApi.Abstractions.Interfaces.Access;

public interface IUserAuthenticationService
{
    Task<Result<AuthResponse>> RegisterUserAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginUserAsync(LoginRequest request, CancellationToken ct = default);
}