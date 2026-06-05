using Conduit.Application.Common;

namespace Conduit.Application.Users;

public sealed class RegisterUserHandler(IUserAccountStore users, ITokenIssuer tokens)
{
    public async Task<Result<AuthenticatedUser>> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = Validate(command);
        if (validationErrors.Count > 0)
        {
            return Result<AuthenticatedUser>.Fail(ErrorKind.Validation, validationErrors);
        }

        var createResult = await users.CreateAsync(
            command.UserName.Trim(),
            command.Email.Trim(),
            command.Password,
            cancellationToken);

        if (!createResult.IsSuccess)
        {
            return Result<AuthenticatedUser>.Fail(createResult.Kind, createResult.Errors);
        }

        var token = tokens.IssueToken(createResult.Value!);
        return Result<AuthenticatedUser>.Ok(new AuthenticatedUser(createResult.Value!, token));
    }

    private static IReadOnlyDictionary<string, string[]> Validate(RegisterUserCommand command)
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrWhiteSpace(command.UserName))
        {
            errors.TryAdd("username", []);
            errors["username"].Add("can't be blank");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.TryAdd("email", []);
            errors["email"].Add("can't be blank");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors.TryAdd("password", []);
            errors["password"].Add("can't be blank");
        }

        return errors.ToDictionary(
            e => e.Key,
            e => e.Value.ToArray());
    }
}
