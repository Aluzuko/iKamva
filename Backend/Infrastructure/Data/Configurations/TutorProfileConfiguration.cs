using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class TutorProfileConfiguration : IEntityTypeConfiguration<TutorProfile>
{
    public void Configure(EntityTypeBuilder<TutorProfile> builder)
    {
        builder.ToTable("TutorProfiles");
        builder.HasKey(t => t.UserId);

        builder.HasOne(t => t.User)
            .WithOne()
            .HasForeignKey<TutorProfile>(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.Bio).HasMaxLength(2000);
        builder.Property(t => t.HourlyRate).HasColumnType("numeric(10,2)");
        builder.Property(t => t.RejectionReason).HasMaxLength(500);

        builder.Property(t => t.VerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
    }
}
