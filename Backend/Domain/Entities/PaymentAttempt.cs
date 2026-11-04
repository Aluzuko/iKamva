using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public class PaymentAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Guid ParentUserId { get; set; }
    public decimal Amount { get; set; }
    public required string Provider { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ProviderPaymentId { get; set; }
    public string? ProviderStatus { get; set; }
    public decimal? AmountFee { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
