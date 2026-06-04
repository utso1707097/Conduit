using Microsoft.EntityFrameworkCore;

namespace Conduit.Infrastructure.Persistence;

public class ConduitDbContext : DbContext
{
    public ConduitDbContext(DbContextOptions<ConduitDbContext> options) : base(options) { }
}
