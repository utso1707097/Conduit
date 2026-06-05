using Conduit.Application.Users;

namespace Conduit.Application.Tests.Fakes;

internal sealed class FakeTokenIssuer : ITokenIssuer
{
    public string IssueToken(UserAccount user) => $"test-token-for-{user.Id}";
}
