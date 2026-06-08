using Conduit.Application.Common;
using Conduit.Application.Users;

namespace Conduit.Application.Tests.Fakes;

internal sealed class FakeUserAccountStore : IUserAccountStore
{
    private readonly Dictionary<string, UserAccount> _byEmail = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, UserAccount> _byUserName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _passwords = new(StringComparer.OrdinalIgnoreCase);

    public Func<string, string, string, Result<UserAccount>>? CreateHandler { get; set; }

    public Task<UserAccount?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var account = _byEmail.Values.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(account);
    }

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

    public Task<Result<UserAccount>> UpdateAsync(
        string userId,
        UpdateUserChanges changes,
        CancellationToken cancellationToken = default)
    {
        var account = _byEmail.Values.FirstOrDefault(u => u.Id == userId);
        if (account is null)
        {
            return Task.FromResult(Result<UserAccount>.NotFound("user", "was not found"));
        }

        if (changes.Email is not null)
        {
            if (_byEmail.ContainsKey(changes.Email) && _byEmail[changes.Email].Id != userId)
            {
                return Task.FromResult(Result<UserAccount>.Conflict("email", "has already been taken"));
            }

            var oldEmail = account.Email;
            _byEmail.Remove(oldEmail);
            account = account with { Email = changes.Email };
            _byEmail[changes.Email] = account;
            if (_passwords.Remove(oldEmail, out var password))
            {
                _passwords[changes.Email] = password;
            }
        }

        if (changes.UserName is not null)
        {
            if (_byUserName.ContainsKey(changes.UserName) && _byUserName[changes.UserName].Id != userId)
            {
                return Task.FromResult(Result<UserAccount>.Conflict("username", "has already been taken"));
            }

            _byUserName.Remove(account.UserName);
            account = account with { UserName = changes.UserName };
            _byUserName[changes.UserName] = account;
        }

        if (changes.Password is not null)
        {
            _passwords[account.Email] = changes.Password;
        }

        if (changes.Bio is not null)
        {
            account = account with { Bio = string.IsNullOrWhiteSpace(changes.Bio) ? null : changes.Bio };
        }

        if (changes.Image is not null)
        {
            account = account with { Image = string.IsNullOrWhiteSpace(changes.Image) ? null : changes.Image };
        }

        _byEmail[account.Email] = account;
        _byUserName[account.UserName] = account;
        return Task.FromResult(Result<UserAccount>.Ok(account));
    }
}
