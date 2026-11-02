using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enums;

namespace Backend.Models.Tutor;

public sealed class TutorLessonDto
{
    public required Guid BookingId { get; init; }
    public required string StudentName { get; init; }
    public required string SubjectName { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public required int DurationMinutes { get; init; }
    public required decimal Price { get; init; }
    public required decimal Earning { get; init; }
    public required BookingStatus Status { get; init; }
    public string? Notes { get; init; }
    public string? RecordingUrl { get; init; }
    public string? CancellationReason { get; init; }
    public required bool NeedsCompletion { get; init; }
    public int? Rating { get; init; }
}

public sealed class CancelLessonRequest
{
    [Required, MinLength(5), MaxLength(500)]
    public required string Reason { get; init; }
}

public sealed class UpdateLessonNotesRequest
{
    [Required, MinLength(1), MaxLength(4000)]
    public required string Notes { get; init; }

    [MaxLength(500), Url]
    public string? RecordingUrl { get; init; }
}

public sealed class TutorDashboardResponse
{
    public required string VerificationState { get; init; }
    public string? RejectionReason { get; init; }
    public required IReadOnlyList<TutorLessonDto> TodayLessons { get; init; }
    public TutorLessonDto? NextLesson { get; init; }
    public required int UpcomingThisWeek { get; init; }
    public required int CompletedThisMonth { get; init; }
    public required decimal EarningsThisMonth { get; init; }
    public required decimal EarningsChangePercent { get; init; }
    public decimal? AverageRating { get; init; }
    public required int ReviewCount { get; init; }
    public required int LessonsNeedingNotes { get; init; }
    public required int LessonsNeedingCompletion { get; init; }
    public required bool HasAvailability { get; init; }
}

public sealed class ReviewDto
{
    public required Guid ReviewId { get; init; }
    public required string StudentName { get; init; }
    public required string SubjectName { get; init; }
    public required int Rating { get; init; }
    public string? Comment { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime LessonDate { get; init; }
}

public sealed class FeedbackResponse
{
    public decimal? AverageRating { get; init; }
    public required int ReviewCount { get; init; }
    public required IReadOnlyList<int> Distribution { get; init; }
    public required IReadOnlyList<ReviewDto> Reviews { get; init; }
}
