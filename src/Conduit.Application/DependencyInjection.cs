using Conduit.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Conduit.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterUserHandler>();
        services.AddScoped<LoginUserHandler>();
        services.AddScoped<RefreshUserTokenHandler>();
        services.AddScoped<RevokeRefreshTokenHandler>();
        services.AddScoped<ListUserRefreshTokensHandler>();
        return services;
    }
}
