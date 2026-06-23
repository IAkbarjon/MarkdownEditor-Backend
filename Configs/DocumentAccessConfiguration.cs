using MarkdownEditor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarkdownEditor.Configs
{
    public class DocumentAccessConfiguration : IEntityTypeConfiguration<DocumentAccess>
    {
        public void Configure(EntityTypeBuilder<DocumentAccess> builder)
        {
            builder.ToTable("documents_accesses");

            builder.HasKey(da => da.Id);

            builder
                .HasOne(da => da.User)
                .WithMany(u => u.AccessToDocuments)
                .HasForeignKey(da => da.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder
                .HasOne(da => da.Document)
                .WithMany(d => d.DocumentAccesses)
                .HasForeignKey(da => da.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
