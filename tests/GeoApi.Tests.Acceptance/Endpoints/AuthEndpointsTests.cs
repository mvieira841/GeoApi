using FluentAssertions;
using GeoApi.Abstractions.Requests.Auth;
using GeoApi.Abstractions.Responses.Auth;
using GeoApi.Access.Persistence;
using GeoApi.Host.Endpoints.Constants;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using static GeoApi.Tests.Acceptance.CustomWebApplicationFactory;

namespace GeoApi.Tests.Acceptance.Endpoints;

public class AuthEndpointsTests(NestedWebAppFactory factory) : CustomWebApplicationFactory(factory)
{
    private static readonly string AuthBase = $"{BaseApi}{AuthEndpointsConstants.Paths.Main}";
    private static readonly string RegisterUrl = $"{AuthBase}{AuthEndpointsConstants.Paths.Register}";

    [Fact]
    public async Task POST_Register_ShouldReturnToken_WhenRequestIsValid()
    {
        // Arrange
        var client = GetPublicClient();
        var request = new RegisterRequest(
            "Acceptance", "Test", $"accept-user-{Guid.NewGuid()}",
            $"accept-{Guid.NewGuid()}@test.com", "Password123!");

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Email.Should().Be(request.Email);
        authResponse.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_Register_ShouldReturnConflict_WhenEmailExists()
    {
        // Arrange
        var client = GetPublicClient();
        var request = new RegisterRequest(
            "Test", "User", "new-user", DataSeeder.AdminEmail, "Password123!");

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_Register_ShouldReturnValidationProblem_WhenRequiredFieldsAreEmpty()
    {
        // Arrange
        var client = GetPublicClient();
        var request = new RegisterRequest("", "", "", "", "");

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("One or more validation errors occurred.");
        problem.Errors.Should().ContainKeys("FirstName", "LastName", "UserName", "Email", "Password");
        problem.Errors["FirstName"][0].Should().Be("'First Name' must not be empty.");
        problem.Errors["Email"][0].Should().Be("'Email' must not be empty.");
    }

    [Fact]
    public async Task POST_Register_ShouldReturnValidationProblem_WhenFieldsExceedMaxLength()
    {
        // Arrange
        var client = GetPublicClient();
        var longString = new string('a', 51);
        var request = new RegisterRequest(
            longString,
            longString,
            longString,
            "valid@email.com",
            "Password123!");

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKeys("FirstName", "LastName", "UserName");
        problem.Errors["FirstName"][0].Should().Contain("must be 50 characters or fewer");
        problem.Errors["LastName"][0].Should().Contain("must be 50 characters or fewer");
        problem.Errors["UserName"][0].Should().Contain("must be 50 characters or fewer");
    }

    [Fact]
    public async Task POST_Register_ShouldReturnValidationProblem_WhenEmailIsInvalid()
    {
        // Arrange
        var client = GetPublicClient();
        var request = new RegisterRequest("Test", "User", "testuser", "invalid-email", "Password123!");

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("One or more validation errors occurred.");
        problem.Errors.Should().ContainKey("Email");
        problem.Errors["Email"][0].Should().Be("'Email' is not a valid email address.");
    }

    [Theory]
    [InlineData("1234", new[] { "at least 8 characters", "one uppercase letter", "one lowercase letter" })]
    [InlineData("password", new[] { "Password must contain one uppercase letter.", "Password must contain one number." })]
    [InlineData("PasswordABC!", new[] { "one number" })]
    [InlineData("password123!", new[] { "one uppercase letter" })]
    [InlineData("PASSWORD123!", new[] { "one lowercase letter" })]
    public async Task POST_Register_ShouldReturnValidationProblem_WhenPasswordIsInvalid(string password, string[] expectedErrors)
    {
        // Arrange
        var client = GetPublicClient();
        var request = new RegisterRequest("Test", "User", $"weak-pw-user-{Guid.NewGuid()}", $"weak-{Guid.NewGuid()}@test.com", password);

        // Act
        var response = await client.PostAsJsonAsync(RegisterUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("Password");

        var allPasswordErrors = problem.Errors.Where(kvp => kvp.Key.StartsWith("Password")).SelectMany(kvp => kvp.Value).ToArray();

        // Check that all expected error messages are present
        foreach (var error in expectedErrors)
        {
            allPasswordErrors.Should().Contain(msg => msg.Contains(error));
        }
    }

    [Fact]
    public async Task POST_Login_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var client = GetPublicClient();
        var request = new LoginRequest(DataSeeder.AdminUserName, DataSeeder.AdminPassword);

        // Act
        var response = await client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task POST_Login_ShouldReturnValidationProblem_WhenFieldsAreEmpty()
    {
        // Arrange
        var client = GetPublicClient();
        var request = new LoginRequest("", "");

        // Act
        var response = await client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("One or more validation errors occurred.");
        problem.Errors.Should().ContainKeys("UserName", "Password");
        problem.Errors["UserName"][0].Should().Be("'User Name' must not be empty.");
        problem.Errors["Password"][0].Should().Be("'Password' must not be empty.");
    }

    [Fact]
    public async Task POST_Login_ShouldReturnUnauthorized_WhenPasswordIsInvalid()
    {
        // Arrange
        var client = GetPublicClient();
        var request = new LoginRequest(DataSeeder.AdminUserName, "WrongPassword!");

        // Act
        var response = await client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Login_ShouldReturnUnauthorized_WhenUserDoesNotExist()
    {
        // Arrange
        var client = GetPublicClient();
        var request = new LoginRequest("NonExistentUser", "Password123!");

        // Act
        var response = await client.PostAsJsonAsync(LoginUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}