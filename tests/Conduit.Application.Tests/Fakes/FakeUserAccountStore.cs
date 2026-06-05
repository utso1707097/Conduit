using Conduit.Application.Common;
using Conduit.Application.Users;

namespace Conduit.Application.Tests.Fakes;

internal sealed class FakeUserAccountStore : IUserAccountStore
{
    private readonly Dictionary<string, UserAccount> _byEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, UserAccount> _byUserName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _passwords = new(StringComparer.OrdinalIgnoreCase);

    public Func<string, string, string, Result<UserAccount>>? CreateHandler { get; set; }

    public Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byEmail.GetValueOrDefault(email));

    public Task<UserAccount?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byUserName.GetValueOrDefault(userName));

    public Task<Result<UserAccount>> CreateAsync(
        string userName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (CreateHandler is not null)
        {
            return Task.FromResult(CreateHandler(userName, email, password));
        }

        if (_byEmail.ContainsKey(email))
        {
            return Task.FromResult(Result<UserAccount>.Conflict("email", "has already been taken"));
        }

        if (_byUserName.ContainsKey(userName))
        {
            return Task.FromResult(Result<UserAccount>.Conflict("username", "has already been taken"));
        }

        var account = new UserAccount(Guid.NewGuid().ToString(), userName, email, null, null);
        _byEmail[email] = account;
        _byUserName[userName] = account;
        _passwords[email] = password;
        return Task.FromResult(Result<UserAccount>.Ok(account));
    }

    public Task<UserAccount?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!_byEmail.TryGetValue(email, out var account))
        {
            return Task.FromResult<UserAccount?>(null);
        }

        if (!_passwords.TryGetValue(email, out var stored) || stored != password)
        {
            return Task.FromResult<UserAccount?>(null);
        }

        return Task.FromResult<UserAccount?>(account);
    }
}
