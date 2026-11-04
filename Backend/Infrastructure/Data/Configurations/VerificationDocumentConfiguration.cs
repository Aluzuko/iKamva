using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class VerificationDocumentConfiguration : IEntityTypeConfiguration<VerificationDocument>
{
    public void Configure(EntityTypeBuilder<VerificationDocument> builder)
    {
        builder.ToTable("VerificationDocuments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName).IsRequired().HasMaxLength(255);
        builder.Property(d => d.FileUrl).IsRequired().HasMaxLength(1000);
        builder.Property(d => d.ContentType).HasMaxLength(100);
        builder.HasIndex(d => new { d.TutorUserId, d.DocumentType }).IsUnique();

        builder.Property(d => d.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(d => d.TutorProfile)
            .WithMany(t => t.Documents)
            .HasForeignKey(d => d.TutorUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
