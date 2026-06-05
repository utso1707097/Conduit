using Conduit.Infrastructure.Constants;
using Conduit.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Conduit.Infrastructure.Persistence;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(IdentityDataSeeder));

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Enum.GetValues<Authorization.Roles>())
        {
            var roleName = role.ToString();
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                logger.LogWarning(
                    "Failed to create role {Role}: {Errors}",
                    roleName,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        var existingUser = await userManager.FindByNameAsync(Authorization.DefaultUsername);
        if (existingUser is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = Authorization.DefaultUsername,
            Email = Authorization.DefaultEmail,
            FirstName = "Default",
            LastName = "User",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, Authorization.DefaultPassword);
        if (!createResult.Succeeded)
        {
            logger.LogWarning(
                "Failed to create default user: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(user, Authorization.DefaultRole.ToString());
        if (!roleResult.Succeeded)
        {
            logger.LogWarning(
                "Failed to assign role to default user: {Errors}",
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }
}
