using System.Text.Json.Serialization;
using Conduit.Application.Common;

namespace Conduit.Application.Users;

public sealed record UpdateCurrentUserCommand
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("username")]
    public string? UserName { get; init; }

    [JsonPropertyName("password")]
    public string? Password { get; init; }

    [JsonPropertyName("bio")]
    public string? Bio { get; init; }

    [JsonPropertyName("image")]
    public string? Image { get; init; }
}

public sealed record UpdateCurrentUserRequest(string UserId, UpdateCurrentUserCommand Command);

public sealed class UpdateCurrentUserHandler(IUserAccountStore users, ITokenIssuer tokens)
{
    public async Task<Result<UserWithToken>> HandleAsync(
        UpdateCurrentUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Result<UserWithToken>.Fail(
                ErrorKind.Unauthorized,
                new Dictionary<string, string[]>
                {
                    ["user"] = ["is not authenticated"]
                });
        }

        var validationErrors = Validate(request.Command);
        if (validationErrors.Count > 0)
        {
            return Result<UserWithToken>.Fail(ErrorKind.Validation, validationErrors);
        }

        var changes = ToChanges(request.Command);
        if (!HasChanges(changes))
        {
            return Result<UserWithToken>.Validation("user", "must include at least one field to update");
        }

        var updateResult = await users.UpdateAsync(request.UserId, changes, cancellationToken);
        if (!updateResult.IsSuccess)
        {
            return Result<UserWithToken>.Fail(updateResult.Kind, updateResult.Errors);
        }

        var account = updateResult.Value!;
        var token = tokens.IssueToken(account);
        return Result<UserWithToken>.Ok(new UserWithToken(account, token));
    }

    private static UpdateUserChanges ToChanges(UpdateCurrentUserCommand command) =>
        new(
            Email: command.Email is null ? null : command.Email.Trim(),
            UserName: command.UserName is null ? null : command.UserName.Trim(),
            Password: command.Password,
            Bio: command.Bio,
            Image: command.Image);

    private static bool HasChanges(UpdateUserChanges changes) =>
        changes.Email is not null
        || changes.UserName is not null
        || changes.Password is not null
        || changes.Bio is not null
        || changes.Image is not null;

    private static IReadOnlyDictionary<string, string[]> Validate(UpdateCurrentUserCommand command)
    {
        var errors = new Dictionary<string, List<string>>();

        if (command.Email is not null && string.IsNullOrWhiteSpace(command.Email))
        {
            errors.TryAdd("email", []);
            errors["email"].Add("can't be blank");
        }

        if (command.UserName is not null && string.IsNullOrWhiteSpace(command.UserName))
        {
            errors.TryAdd("username", []);
            errors["username"].Add("can't be blank");
        }

        if (command.Password is not null && string.IsNullOrWhiteSpace(command.Password))
        {
            errors.TryAdd("password", []);
            errors["password"].Add("can't be blank");
        }

        return errors.ToDictionary(
            e => e.Key,
            e => e.Value.ToArray());
    }
}
