using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public class Payout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TutorUserId { get; set; }
    public decimal Amount { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Requested;
    public string? Reference { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}
