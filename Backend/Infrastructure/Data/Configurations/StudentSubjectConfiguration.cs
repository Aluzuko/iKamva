using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class StudentSubjectConfiguration : IEntityTypeConfiguration<StudentSubject>
{
    public void Configure(EntityTypeBuilder<StudentSubject> builder)
    {
        builder.ToTable("StudentSubjects");
        builder.HasKey(s => new { s.StudentUserId, s.SubjectId });

        builder.HasOne(s => s.StudentProfile)
            .WithMany(p => p.Subjects)
            .HasForeignKey(s => s.StudentUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Subject)
            .WithMany()
            .HasForeignKey(s => s.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}