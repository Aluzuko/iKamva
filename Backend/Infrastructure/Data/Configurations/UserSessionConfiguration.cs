using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RefreshTokenHash)
            .IsRequired();

        builder.HasIndex(s => s.RefreshTokenHash)
            .IsUnique();

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired();

        builder.Property(s => s.IpAddress).HasMaxLength(64);
        builder.Property(s => s.UserAgent).HasMaxLength(512);
        builder.Property(s => s.RevokedReason).HasMaxLength(200);

        builder.Property(s => s.DataJson)
            .HasColumnType("jsonb");

        builder.HasIndex(s => s.UserId);
    }
}
