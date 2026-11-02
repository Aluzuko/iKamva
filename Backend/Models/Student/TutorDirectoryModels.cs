using Backend.Models.Tutor;

namespace Backend.Models.Student;

public sealed class SubjectRefDto
{
    public required Guid SubjectId { get; init; }
    public required string Name { get; init; }
}

public sealed class TutorCardDto
{
    public required Guid TutorId { get; init; }
    public required string FullName { get; init; }
    public string? Headline { get; init; }
    public required decimal HourlyRate { get; init; }
    public required int YearsExperience { get; init; }
    public required IReadOnlyList<SubjectRefDto> Subjects { get; init; }
    public decimal? AverageRating { get; init; }
    public required int ReviewCount { get; init; }
    public required int CompletedLessons { get; init; }
    public required int WeeklyHours { get; init; }
}

public sealed class TutorPublicProfileDto
{
    public required Guid TutorId { get; init; }
    public required string FullName { get; init; }
    public string? Bio { get; init; }
    public required decimal HourlyRate { get; init; }
    public required int YearsExperience { get; init; }
    public required IReadOnlyList<SubjectRefDto> Subjects { get; init; }
    public decimal? AverageRating { get; init; }
    public required int ReviewCount { get; init; }
    public required int CompletedLessons { get; init; }
    public required IReadOnlyList<AvailabilitySlotDto> Availability { get; init; }
    public required IReadOnlyList<ReviewDto> Reviews { get; init; }
}

public sealed class SlotDayDto
{
    public required string Date { get; init; }
    public required IReadOnlyList<DateTime> Starts { get; init; }
}

public sealed class SlotsResponse
{
    public required string TimeZone { get; init; }
    public required int DurationMinutes { get; init; }
    public required IReadOnlyList<SlotDayDto> Days { get; init; }
}
