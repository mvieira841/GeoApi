using FluentAssertions;
using FluentResults;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Interfaces.Managers;
using GeoApi.Abstractions.Requests.Auth;
using GeoApi.Abstractions.Responses.Auth;
using GeoApi.Manager.Managers;
using NSubstitute;

namespace GeoApi.Tests.Unit.Managers;

public class AuthManagerTests
{
    private readonly IAuthManager _sut;
    private readonly IUserAuthenticationService _userAuthService = Substitute.For<IUserAuthenticationService>();
    private readonly CancellationToken _ct = CancellationToken.None;

    public AuthManagerTests()
    {
        _sut = new AuthManager(_userAuthService);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldReturnSuccess_WhenServiceSucceeds()
    {
        var request = new RegisterRequest("Test", "User", "testuser", "test@test.com", "Password123!");
        var authResponse = new AuthResponse(request.Email, request.UserName, "token");
        _userAuthService.RegisterUserAsync(request, _ct).Returns(Result.Ok(authResponse));

        var result = await _sut.RegisterUserAsync(request, _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(authResponse);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldReturnFailure_WhenServiceFails()
    {
        var request = new RegisterRequest("Test", "User", "testuser", "test@test.com", "Password123!");
        var serviceError = new Error("Email already exists.");
        _userAuthService.RegisterUserAsync(request, _ct).Returns(Result.Fail(serviceError));

        var result = await _sut.RegisterUserAsync(request, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == serviceError);
    }

    [Fact]
    public async Task LoginUserAsync_ShouldReturnSuccess_WhenServiceSucceeds()
    {
        var request = new LoginRequest("test@test.com", "Password123!");
        var authResponse = new AuthResponse(request.UserName, request.UserName, "token");
        _userAuthService.LoginUserAsync(request, _ct).Returns(Result.Ok(authResponse));

        var result = await _sut.LoginUserAsync(request, _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(authResponse);
    }


    [Fact]
    public async Task LoginUserAsync_ShouldReturnFailure_WhenCredentialsAreInvalid()
    {
        var request = new LoginRequest("test@test.com", "wrongpass");
        var serviceError = new Error("Invalid email or password.");
        _userAuthService.LoginUserAsync(request, _ct).Returns(Result.Fail(serviceError));

        var result = await _sut.LoginUserAsync(request, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e == serviceError);
    }
}