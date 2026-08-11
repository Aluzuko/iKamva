namespace Backend.Models.Parent;

public sealed class ChildSummaryDto
{
    public required Guid StudentId { get; init; }
    public required string FullName { get; init; }
    public string? YearLevel { get; init; }
    public required int UpcomingCount { get; init; }
    public required int CompletedCount { get; init; }
    public required IReadOnlyList<string> Subjects { get; init; }
}