namespace Backend.Models.Parent;

public sealed class StudentDetailResponse
{
    public required Guid StudentId { get; init; }
    public required string FullName { get; init; }
    public string? YearLevel { get; init; }
    public required IReadOnlyList<string> EnrolledSubjects { get; init; }
    public required bool IsActive { get; init; }
    public required int UpcomingCount { get; init; }
    public required int CompletedCount { get; init; }
    public double? AvgRating { get; init; }
    public required IReadOnlyList<UpcomingLessonDto> UpcomingLessons { get; init; }
    public required IReadOnlyList<LessonHistoryItemDto> LessonHistory { get; init; }
}