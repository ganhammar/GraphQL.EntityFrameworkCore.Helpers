using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure
{
    public class DifferentTestDbContext : DbContext
    {
        public DifferentTestDbContext()
        { }

        public DifferentTestDbContext(DbContextOptions<DifferentTestDbContext> options)
            : base(options)
        { }

        public DbSet<Galaxy> Galaxies { get; set; }
    }
}