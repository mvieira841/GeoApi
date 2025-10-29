using FluentResults;
using GeoApi.Abstractions.Requests.Auth;
using GeoApi.Abstractions.Responses.Auth;

namespace GeoApi.Abstractions.Interfaces.Managers;

public interface IAuthManager
{
    Task<Result<AuthResponse>> RegisterUserAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginUserAsync(LoginRequest request, CancellationToken ct = default);
}