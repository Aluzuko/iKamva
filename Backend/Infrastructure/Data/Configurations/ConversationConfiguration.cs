using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.LastMessagePreview).HasMaxLength(200);

        builder.HasIndex(c => new { c.TutorUserId, c.OtherUserId }).IsUnique();
        builder.HasIndex(c => c.OtherUserId);
    }
}
