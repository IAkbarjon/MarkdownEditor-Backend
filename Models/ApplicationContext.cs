using MarkdownEditor.Configs;
using Microsoft.EntityFrameworkCore;

namespace MarkdownEditor.Models
{
    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentAccess> DocumentAccesses { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new DocumentConfiguration());
            modelBuilder.ApplyConfiguration(new DocumentAccessConfiguration());
            modelBuilder.ApplyConfiguration(new DocumentVersionConfiguration());
        }
    }
}
