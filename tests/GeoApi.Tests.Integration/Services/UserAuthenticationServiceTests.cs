using FluentAssertions;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Requests.Auth;
using GeoApi.Access.Persistence.Context;
using GeoApi.Tests.Integration.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GeoApi.Tests.Integration.Services;

public class UserAuthenticationServiceTests
    : CustomTestWebAppFactory
{
    private readonly IUserAuthenticationService _sut;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CancellationToken _ct = CancellationToken.None;

    public UserAuthenticationServiceTests(NestedWebAppFactory factory) : base(factory)
    {
        _sut = ServiceProvider.GetRequiredService<IUserAuthenticationService>();
        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldCreateUser_WhenRequestIsValid()
    {
        var request = new RegisterRequest("Integration", "Test", "integ-test", "integ@test.com", "Password123!");

        var result = await _sut.RegisterUserAsync(request, _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(request.Email);
        result.Value.Token.Should().NotBeNullOrEmpty();

        var user = await _userManager.FindByEmailAsync(request.Email);
        user.Should().NotBeNull();
        user!.UserName.Should().Be(request.UserName);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldFail_WhenEmailExists()
    {
        var request1 = new RegisterRequest("User", "One", "user1", "user@test.com", "Password123!");
        var request2 = new RegisterRequest("User", "Two", "user2", "user@test.com", "Password123!");
        await _sut.RegisterUserAsync(request1, _ct);

        var result = await _sut.RegisterUserAsync(request2, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "User with this email already exists.");
        result.Errors.First().Metadata["ErrorType"].Should().Be("Conflict");
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldFail_WhenUsernameExists()
    {
        var request1 = new RegisterRequest("User", "One", "unique-user", "user1@test.com", "Password123!");
        var request2 = new RegisterRequest("User", "Two", "unique-user", "user2@test.com", "Password123!");
        await _sut.RegisterUserAsync(request1, _ct);

        var result = await _sut.RegisterUserAsync(request2, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Metadata["PropertyName"].ToString() == "DuplicateUserName");
        result.Errors.First().Metadata["ErrorType"].Should().Be("Validation");
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldFail_WhenPasswordIsWeak()
    {
        var request = new RegisterRequest("Weak", "Pass", "weak-pass", "weak@test.com", "123");

        var result = await _sut.RegisterUserAsync(request, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Metadata["PropertyName"].ToString() == "PasswordTooShort");
        result.Errors.Should().Contain(e => e.Metadata["PropertyName"].ToString() == "PasswordRequiresUpper");
        result.Errors.First().Metadata["ErrorType"].Should().Be("Validation");
    }

    [Fact]
    public async Task LoginUserAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        var request = new RegisterRequest("Login", "Test", "login-test", "login@test.com", "Password123!");
        await _sut.RegisterUserAsync(request, _ct);
        var loginRequest = new LoginRequest(request.UserName, request.Password);

        var result = await _sut.LoginUserAsync(loginRequest, _ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(request.Email);
        result.Value.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginUserAsync_ShouldFail_WhenPasswordIsIncorrect()
    {
        var request = new RegisterRequest("Login", "Fail", "login-fail", "loginfail@test.com", "Password123!");
        await _sut.RegisterUserAsync(request, _ct);
        var loginRequest = new LoginRequest(request.UserName, "WrongPassword!");

        var result = await _sut.LoginUserAsync(loginRequest, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "Invalid email or password.");
        result.Errors.First().Metadata["ErrorType"].Should().Be("Unauthorized");
    }

    [Fact]
    public async Task LoginUserAsync_ShouldFail_WhenUserDoesNotExist()
    {
        var loginRequest = new LoginRequest("non-existent-user", "Password123!");

        var result = await _sut.LoginUserAsync(loginRequest, _ct);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message == "Invalid username or password.");
        result.Errors.First().Metadata["ErrorType"].Should().Be("Unauthorized");
    }
}