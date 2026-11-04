using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Data.Configurations;

public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("PaymentAttempts");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasColumnType("numeric(10,2)");
        builder.Property(p => p.AmountFee).HasColumnType("numeric(10,2)");
        builder.Property(p => p.Provider).IsRequired().HasMaxLength(20);
        builder.Property(p => p.ProviderPaymentId).HasMaxLength(50);
        builder.Property(p => p.ProviderStatus).HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(p => p.Invoice)
            .WithMany()
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.InvoiceId);
        builder.HasIndex(p => p.ParentUserId);
    }
}
