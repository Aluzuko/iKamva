using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public class Dispute
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BookingId { get; set; }
    public Guid RaisedByUserId { get; set; }
    public required string Subject { get; set; }
    public required string Description { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
