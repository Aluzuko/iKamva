using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("EmailMessages");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Priority).HasConversion<int>();

        builder.Property(e => e.ToEmail).IsRequired().HasMaxLength(320);
        builder.Property(e => e.ToName).HasMaxLength(200);
        builder.Property(e => e.CcEmails).HasMaxLength(2000);
        builder.Property(e => e.BccEmails).HasMaxLength(2000);
        builder.Property(e => e.FromEmail).HasMaxLength(320);
        builder.Property(e => e.FromName).HasMaxLength(200);
        builder.Property(e => e.ReplyToEmail).HasMaxLength(320);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(300);
        builder.Property(e => e.HtmlBody).IsRequired();
        builder.Property(e => e.RelatedEntityType).HasMaxLength(50);
        builder.Property(e => e.IdempotencyKey).HasMaxLength(200);
        builder.Property(e => e.LastError).HasMaxLength(2000);
        builder.Property(e => e.LockedBy).HasMaxLength(100);
        builder.Property(e => e.ProviderMessageId).HasMaxLength(200);

        builder.HasIndex(e => new { e.Status, e.NextAttemptAt, e.Priority });
        builder.HasIndex(e => e.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        builder.HasIndex(e => new { e.RelatedEntityType, e.RelatedEntityId });
        builder.HasIndex(e => e.RecipientUserId);
        builder.HasIndex(e => e.CreatedAt);
    }
}

public class EmailAttachmentConfiguration : IEntityTypeConfiguration<EmailAttachment>
{
    public void Configure(EntityTypeBuilder<EmailAttachment> builder)
    {
        builder.ToTable("EmailAttachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(a => a.ContentId).HasMaxLength(100);

        builder.HasOne(a => a.EmailMessage)
            .WithMany(m => m.Attachments)
            .HasForeignKey(a => a.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
