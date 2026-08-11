using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public static readonly Guid Mathematics = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid Physics = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid ComputerScience = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static readonly Guid EnglishLiterature = Guid.Parse("00000000-0000-0000-0000-000000000004");
    public static readonly Guid French = Guid.Parse("00000000-0000-0000-0000-000000000005");

    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => s.Name).IsUnique();

        builder.HasData(
            new Subject { Id = Mathematics, Name = "Mathematics" },
            new Subject { Id = Physics, Name = "Physics" },
            new Subject { Id = ComputerScience, Name = "Computer Science" },
            new Subject { Id = EnglishLiterature, Name = "English Literature" },
            new Subject { Id = French, Name = "French" }
        );
    }
}