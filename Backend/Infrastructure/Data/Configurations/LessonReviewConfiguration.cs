using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class LessonReviewConfiguration : IEntityTypeConfiguration<LessonReview>
{
    public void Configure(EntityTypeBuilder<LessonReview> builder)
    {
        builder.ToTable("LessonReviews", t => t.HasCheckConstraint("CK_LessonReviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Comment).HasMaxLength(2000);

        builder.HasOne(r => r.Booking)
            .WithMany()
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.BookingId).IsUnique();
        builder.HasIndex(r => r.TutorUserId);
    }
}
