using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class ParentStudentLinkConfiguration : IEntityTypeConfiguration<ParentStudentLink>
{
    public void Configure(EntityTypeBuilder<ParentStudentLink> builder)
    {
        builder.ToTable("ParentStudentLinks");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.InviteEmail).HasMaxLength(320);
        builder.Property(l => l.Relationship).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(l => l.ParentUserId);
        builder.HasIndex(l => l.StudentUserId);
        builder.HasIndex(l => l.InviteEmail);
    }
}