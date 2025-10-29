using Asp.Versioning;
using GeoApi.Abstractions.Enums;
using GeoApi.Abstractions.Requests.Auth;
using GeoApi.Access.Settings;
using GeoApi.Host.ApiHelpers;
using GeoApi.Host.Endpoints.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

namespace GeoApi.Host;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddHost(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();

        // Add Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(ApiVersionSet.CurrentVersionString, new OpenApiInfo { Title = "GeoAPI", Version = ApiVersionSet.CurrentVersionString });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "bearer"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
            // Load XML comments from the Host assembly
            var hostAssembly = Assembly.GetExecutingAssembly();
            var hostXmlFile = $"{hostAssembly.GetName().Name}.xml";
            var hostXmlPath = Path.Combine(AppContext.BaseDirectory, hostXmlFile);
            options.IncludeXmlComments(hostXmlPath);

            // Load XML comments from the Abstractions assembly
            var abstractionsAssembly = typeof(RegisterRequest).Assembly;
            var abstractionsXmlFile = $"{abstractionsAssembly.GetName().Name}.xml";
            var abstractionsXmlPath = Path.Combine(AppContext.BaseDirectory, abstractionsXmlFile);
            options.IncludeXmlComments(abstractionsXmlPath);
        });

        // --- Configure Serilog ---
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();
        builder.Host.UseSerilog();

        // Add Http Accessor for Authentication & Authorization
        builder.Services.AddHttpContextAccessor();

        // Add API Versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = ApiVersionSet.CurrentVersion;
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Add Authentication & Authorization
        var jwtSettings = new JwtSettings();
        builder.Configuration.GetSection(JwtSettings.SectionName).Bind(jwtSettings);
        if (string.IsNullOrEmpty(jwtSettings.Issuer) ||
            string.IsNullOrEmpty(jwtSettings.Audience) ||
            string.IsNullOrEmpty(jwtSettings.Key))
        {
            throw new InvalidOperationException(
                $"JWT settings in '{JwtSettings.SectionName}' are not configured correctly. " +
                "Please check your appsettings.json or user secrets.");
        }
        builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection(JwtSettings.SectionName)
        );

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings!.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key!))
            };
        });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthPoliciesConstants.RequireAdminRolePolicy, policy =>
                policy.RequireRole(UserRoleConstants.Admin));
            options.AddPolicy(AuthPoliciesConstants.RequireUserRolePolicy, policy =>
                policy.RequireRole(UserRoleConstants.User, UserRoleConstants.Admin));
        });

        // Configure HTTP Logging
        builder.Services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody |
                                    HttpLoggingFields.RequestHeaders | HttpLoggingFields.ResponseHeaders;
            options.RequestBodyLogLimit = 4096;
            options.ResponseBodyLogLimit = 4096;
            options.CombineLogs = true;
        });

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        return builder;
    }
}