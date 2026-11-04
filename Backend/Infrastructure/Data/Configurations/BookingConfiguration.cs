using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.PriceAmount).HasColumnType("numeric(10,2)");
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.RecordingUrl).HasMaxLength(500);
        builder.Property(b => b.CommissionPercent).HasColumnType("numeric(5,2)");
        builder.Property(b => b.CancellationReason).HasMaxLength(500);

        builder.HasOne(b => b.Subject).WithMany().HasForeignKey(b => b.SubjectId);

        builder.HasIndex(b => b.StudentUserId);
        builder.HasIndex(b => b.TutorUserId);
        builder.HasIndex(b => b.ScheduledAt);
    }
}