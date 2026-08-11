namespace Backend.Models.Parent;

public sealed class FamilyDashboardResponse
{
    public required int ChildrenLinked { get; init; }
    public required int UpcomingLessonsThisWeek { get; init; }
    public required decimal MonthlySpend { get; init; }
    public required decimal MonthlySpendChangePercent { get; init; }
    public required int TotalSessions { get; init; }
    public required decimal TotalSessionsChangePercent { get; init; }
    public required IReadOnlyList<ChildSummaryDto> Children { get; init; }
    public required IReadOnlyList<UpcomingLessonDto> UpcomingLessons { get; init; }
    public InvoiceDto? OutstandingInvoice { get; init; }
}