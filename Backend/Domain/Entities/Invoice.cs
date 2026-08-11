using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParentUserId { get; set; }
    public Guid StudentUserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int LessonCount { get; set; }
    public decimal AmountTotal { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}