using Conduit.Application.Common;
using Conduit.Application.Users;
using Conduit.Infrastructure.Constants;
using Conduit.Infrastructure.Models;
using Conduit.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Conduit.Infrastructure.Users;

public sealed class UserAccountStore(
    UserManager<ApplicationUser> userManager,
    ConduitDbContext db) : IUserAccountStore
{
    public async Task<UserAccount?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id);
        return user is null ? null : ToAccount(user);
    }

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
            // Run a throwaway hash so a missing account costs roughly the same time as
            // a wrong password, denying attackers a user-enumeration timing oracle.
            userManager.PasswordHasher.HashPassword(new ApplicationUser(), password);
            return null;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.ResetAccessFailedCountAsync(user);
            return ToAccount(user);
        }

        // Increments the failure counter and locks the account once the threshold is hit.
        await userManager.AccessFailedAsync(user);
        return null;
    }

    public async Task<Result<UserAccount>> UpdateAsync(
        string userId,
        UpdateUserChanges changes,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Result<UserAccount>.NotFound("user", "was not found");
        }

        // All field updates run inside one transaction. UserManager calls SaveChanges
        // internally per operation, so without this a failure partway through (e.g. the
        // password remove succeeding but add failing) would leave the account corrupt.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (changes.Email is not null)
        {
            var existing = await FindByEmailAsync(changes.Email, cancellationToken);
            if (existing is not null && existing.Id != userId)
            {
                return Result<UserAccount>.Conflict("email", "has already been taken");
            }

            var emailResult = await userManager.SetEmailAsync(user, changes.Email);
            if (!emailResult.Succeeded)
            {
                return Result<UserAccount>.Fail(
                    ErrorKind.Validation,
                    MapIdentityErrors(emailResult.Errors));
            }
        }

        if (changes.UserName is not null)
        {
            var existing = await FindByUserNameAsync(changes.UserName, cancellationToken);
            if (existing is not null && existing.Id != userId)
            {
                return Result<UserAccount>.Conflict("username", "has already been taken");
            }

            var userNameResult = await userManager.SetUserNameAsync(user, changes.UserName);
            if (!userNameResult.Succeeded)
            {
                return Result<UserAccount>.Fail(
                    ErrorKind.Validation,
                    MapIdentityErrors(userNameResult.Errors));
            }
        }

        if (changes.Password is not null)
        {
            var passwordResult = await userManager.RemovePasswordAsync(user);
            if (!passwordResult.Succeeded)
            {
                return Result<UserAccount>.Fail(
                    ErrorKind.Validation,
                    MapIdentityErrors(passwordResult.Errors));
            }

            passwordResult = await userManager.AddPasswordAsync(user, changes.Password);
            if (!passwordResult.Succeeded)
            {
                return Result<UserAccount>.Fail(
                    ErrorKind.Validation,
                    MapIdentityErrors(passwordResult.Errors));
            }
        }

        if (changes.Bio is not null)
        {
            user.Bio = string.IsNullOrWhiteSpace(changes.Bio) ? null : changes.Bio.Trim();
        }

        if (changes.Image is not null)
        {
            user.Image = string.IsNullOrWhiteSpace(changes.Image) ? null : changes.Image.Trim();
        }

        if (changes.Bio is not null || changes.Image is not null)
        {
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return Result<UserAccount>.Fail(
                    ErrorKind.Validation,
                    MapIdentityErrors(updateResult.Errors));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return Result<UserAccount>.Ok(ToAccount(user));
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
