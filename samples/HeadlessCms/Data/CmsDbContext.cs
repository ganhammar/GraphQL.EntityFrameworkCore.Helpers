using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace HeadlessCms.Data
{
    public class CmsDbContext : DbContext
    {
        public DbSet<Page> Pages { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PageTag> PageTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Page>()
                .HasOne(x => x.Editor)
                .WithMany(x => x.Pages)
                .HasForeignKey(x => x.EditorId);

            modelBuilder.Entity<PageTag>()
                .HasKey(x => new { x.PageId, x.TagId });
        }

         protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(string.Concat(
                "Data Source=",
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase),
                    "HeadlessCms.db")));
    }
}