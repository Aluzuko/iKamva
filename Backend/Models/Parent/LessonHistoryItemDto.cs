using Backend.Domain.Enums;

namespace Backend.Models.Parent;

public sealed class LessonHistoryItemDto
{
    public required Guid BookingId { get; init; }
    public required string SubjectName { get; init; }
    public required string TutorName { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public required int DurationMinutes { get; init; }
    public required BookingStatus Status { get; init; }
    public string? Notes { get; init; }
    public string? RecordingUrl { get; init; }
}