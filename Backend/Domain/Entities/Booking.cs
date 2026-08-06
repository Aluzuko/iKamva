using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentUserId { get; set; }
    public Guid TutorUserId { get; set; }
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PriceAmount { get; set; }
    public BookingStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? RecordingUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}