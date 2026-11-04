using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.AmountTotal).HasColumnType("numeric(10,2)");
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne<Booking>()
            .WithMany()
            .HasForeignKey(i => i.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.BookingId).IsUnique();
        builder.HasIndex(i => i.ParentUserId);
        builder.HasIndex(i => i.StudentUserId);
        builder.HasIndex(i => new { i.Year, i.Month });
    }
}