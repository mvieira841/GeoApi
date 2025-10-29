using FluentResults;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Interfaces.Managers;
using GeoApi.Abstractions.Requests.Auth;
using GeoApi.Abstractions.Responses.Auth;

namespace GeoApi.Manager.Managers;

public sealed class AuthManager(IUserAuthenticationService userAuthService)
    : IAuthManager
{
    public async Task<Result<AuthResponse>> RegisterUserAsync(RegisterRequest request, CancellationToken ct = default)
    {
        return await userAuthService.RegisterUserAsync(request, ct);
    }

    public async Task<Result<AuthResponse>> LoginUserAsync(LoginRequest request, CancellationToken ct = default)
    {
        return await userAuthService.LoginUserAsync(request, ct);
    }
}