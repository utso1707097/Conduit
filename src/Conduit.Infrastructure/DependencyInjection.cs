using System.Text;
using Conduit.Application.Settings;
using Conduit.Application.Users;
using Conduit.Infrastructure.Auth;
using Conduit.Infrastructure.Models;
using Conduit.Infrastructure.Persistence;
using Conduit.Infrastructure.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Conduit.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddDbContext<ConduitDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(ConduitDbContext).Assembly.FullName)));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // RealWorld examples and Hurl tests use simple passwords (e.g. "jakejake").
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // Brute-force protection: lock the account after repeated failures.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<ConduitDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserAccountStore, UserAccountStore>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton(TimeProvider.System);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Default to requiring HTTPS for any metadata retrieval; only relax it
                // where explicitly configured (local dev / test) via JWT:RequireHttpsMetadata.
                options.RequireHttpsMetadata =
                    configuration.GetValue("JWT:RequireHttpsMetadata", true);
                options.SaveToken = false;
                options.MapInboundClaims = false;

                // RealWorld sends Authorization: Token <jwt>, not Bearer.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authorization = context.Request.Headers.Authorization.ToString();
                        if (authorization.StartsWith("Token ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = authorization["Token ".Length..].Trim();
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<Microsoft.Extensions.Options.IOptions<JwtSettings>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                if (string.IsNullOrWhiteSpace(jwt.Key))
                {
                    throw new InvalidOperationException(
                        "JWT signing key is not configured. Set 'JWT:Key' via user-secrets " +
                        "or environment variables.");
                }

                // HMAC-SHA256 requires at least a 256-bit (32-byte) key to be secure.
                if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
                {
                    throw new InvalidOperationException(
                        "JWT signing key must be at least 32 bytes (256 bits) for HMAC-SHA256.");
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };
            });

        return services;
    }
}
