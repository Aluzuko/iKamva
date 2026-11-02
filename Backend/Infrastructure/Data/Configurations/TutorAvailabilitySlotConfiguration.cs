using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class TutorAvailabilitySlotConfiguration : IEntityTypeConfiguration<TutorAvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<TutorAvailabilitySlot> builder)
    {
        builder.ToTable("TutorAvailabilitySlots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DayOfWeek).HasConversion<int>();

        builder.HasOne(s => s.TutorProfile)
            .WithMany(t => t.Availability)
            .HasForeignKey(s => s.TutorUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.TutorUserId, s.DayOfWeek });
    }
}
