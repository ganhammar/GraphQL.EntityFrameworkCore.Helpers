using Microsoft.EntityFrameworkCore;

namespace GraphQL.EntityFrameworkCore.Helpers.Tests.Infrastructure
{
    public class TestDbContext : DbContext
    {
        public TestDbContext()
        { }

        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        { }

        public DbSet<Human> Humans { get; set; }
        public DbSet<Droid> Droids { get; set; }
        public DbSet<Planet> Planets { get; set; }
        public DbSet<Force> Forces { get; set; }
        public DbSet<HumanForceAlignment> HumanForceAlignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Force>()
                .HasMany(x => x.HumanForceAlignments)
                .WithOne(x => x.Force)
                .HasForeignKey(x => x.Alignment);

            modelBuilder.Entity<HumanForceAlignment>()
                .HasKey(x => new { x.HumanId, x.Alignment });

            modelBuilder.Entity<Human>()
                .HasKey(x => x.Id);

            modelBuilder.Entity<Force>()
                .HasKey(x => x.Type);
        }
    }
}