using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("StudentProfiles");
        builder.HasKey(s => s.UserId);

        builder.HasOne(s => s.User)
            .WithOne()
            .HasForeignKey<StudentProfile>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.YearLevel).HasMaxLength(50);

        builder.Property(s => s.LinkCode).IsRequired().HasMaxLength(12);
        builder.HasIndex(s => s.LinkCode).IsUnique();
    }
}