using Conduit.Infrastructure.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Infrastructure.Persistence;

public class ConduitDbContext : IdentityDbContext<ApplicationUser>
{
    public ConduitDbContext(DbContextOptions<ConduitDbContext> options) : base(options) { }
}
