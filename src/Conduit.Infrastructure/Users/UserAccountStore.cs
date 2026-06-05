using Conduit.Application.Common;
using Conduit.Application.Users;
using Conduit.Infrastructure.Constants;
using Conduit.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;

namespace Conduit.Infrastructure.Users;

public sealed class UserAccountStore(UserManager<ApplicationUser> userManager) : IUserAccountStore
{
    public async Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : ToAccount(user);
    }

    public async Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(userName);
        return user is null ? null : ToAccount(user);
    }

    public async Task<Result<UserAccount>> CreateAsync(
        string userName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (await FindByEmailAsync(email, cancellationToken) is not null)
        {
            return Result<UserAccount>.Conflict("email", "has already been taken");
        }

        if (await FindByUserNameAsync(userName, cancellationToken) is not null)
        {
            return Result<UserAccount>.Conflict("username", "has already been taken");
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return Result<UserAccount>.Fail(
                ErrorKind.Validation,
                MapIdentityErrors(createResult.Errors));
        }

        var roleResult = await userManager.AddToRoleAsync(
            user,
            Authorization.DefaultRole.ToString());

        if (!roleResult.Succeeded)
        {
            return Result<UserAccount>.Fail(
                ErrorKind.Validation,
                MapIdentityErrors(roleResult.Errors));
        }

        return Result<UserAccount>.Ok(ToAccount(user));
    }

    public async Task<UserAccount?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        var valid = await userManager.CheckPasswordAsync(user, password);
        return valid ? ToAccount(user) : null;
    }

    private static UserAccount ToAccount(ApplicationUser user) =>
        new(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, user.Bio, user.Image);

    private static Dictionary<string, string[]> MapIdentityErrors(
        IEnumerable<IdentityError> errors)
    {
        var mapped = new Dictionary<string, List<string>>();

        foreach (var error in errors)
        {
            var field = error.Code switch
            {
                "DuplicateUserName" => "username",
                "DuplicateEmail" => "email",
                "InvalidEmail" => "email",
                "PasswordTooShort" or "PasswordRequiresDigit" or "PasswordRequiresLower"
                    or "PasswordRequiresUpper" or "PasswordRequiresNonAlphanumeric" => "password",
                _ => "user"
            };

            if (!mapped.TryGetValue(field, out var messages))
            {
                messages = [];
                mapped[field] = messages;
            }

            messages.Add(error.Description);
        }

        return mapped.ToDictionary(e => e.Key, e => e.Value.ToArray());
    }
}
