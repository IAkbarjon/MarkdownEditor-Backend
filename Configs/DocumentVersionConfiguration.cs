using MarkdownEditor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarkdownEditor.Configs
{
    public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
    {
        public void Configure(EntityTypeBuilder<DocumentVersion> builder)
        {
            builder.ToTable("documents_versions");
            builder.HasKey(dv => dv.Id);

            builder.Property(dv => dv.Content)
                .IsRequired()
                .HasColumnType("text")
                .HasDefaultValue("");

            builder.HasOne(dv => dv.Access)
                .WithMany(da => da.SavedVersions)
                .HasForeignKey(dv => dv.AccessId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
