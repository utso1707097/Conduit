using Microsoft.AspNetCore.Identity;

namespace Conduit.Infrastructure.Models;

public sealed class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string? Image { get; set; }
}
