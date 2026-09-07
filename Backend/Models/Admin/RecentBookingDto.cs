using Backend.Domain.Enums;

namespace Backend.Models.Admin;

public sealed class RecentBookingDto
{
    public required Guid BookingId { get; init; }
    public required string SubjectName { get; init; }
    public required string StudentName { get; init; }
    public required string TutorName { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public required decimal Price { get; init; }
    public required BookingStatus Status { get; init; }
}
