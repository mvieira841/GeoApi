using FluentResults;
using GeoApi.Abstractions.Interfaces.Access;
using GeoApi.Abstractions.Requests.Auth;
using GeoApi.Abstractions.Responses.Auth;
using GeoApi.Access.Persistence.Context;
using GeoApi.Access.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GeoApi.Access.Services;

internal sealed class UserAuthenticationService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IOptions<JwtSettings> options)
    : IUserAuthenticationService
{
    public async Task<Result<AuthResponse>> RegisterUserAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var userExists = await userManager.FindByEmailAsync(request.Email);
        if (userExists is not null)
        {
            return Result.Fail(new Error("User with this email already exists.")
                .WithMetadata("ErrorType", "Conflict"));
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.UserName,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new Error(e.Description)
                .WithMetadata("PropertyName", e.Code)
                .WithMetadata("ErrorType", "Validation"));
            return Result.Fail(errors);
        }

        // TODO: assign roles based on business logic
        await userManager.AddToRoleAsync(user, "User");

        var token = await GenerateJwtToken(user);
        return new AuthResponse(user.Email!, user.UserName!, token);
    }

    public async Task<Result<AuthResponse>> LoginUserAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByNameAsync(request.UserName);
        if (user is null)
        {
            return Result.Fail(new Error("Invalid username or password.")
                .WithMetadata("ErrorType", "Unauthorized"));
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return Result.Fail(new Error("Invalid email or password.")
                .WithMetadata("ErrorType", "Unauthorized"));
        }

        var token = await GenerateJwtToken(user);
        return new AuthResponse(user.Email!, user.UserName!, token);
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserName!),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id)
        };

        // Add roles to claims
        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var jwtSettings = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            expires: DateTime.UtcNow.AddHours(Convert.ToDouble(jwtSettings.DurationInHours)),
            claims: claims,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}