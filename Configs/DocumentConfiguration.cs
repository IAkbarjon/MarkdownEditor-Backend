using MarkdownEditor.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace MarkdownEditor.Configs
{
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.ToTable("documents");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Title)
                .IsRequired()
                .HasMaxLength(40);

            builder.Property(d => d.Content)
                .IsRequired()
                .HasColumnType("text")
                .HasDefaultValue("");

            builder.Property(d => d.CreatedAt)
                .IsRequired();

            builder.Property(d => d.LastUpdated)
                .IsRequired();

            builder
                .HasOne(d => d.Owner)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
