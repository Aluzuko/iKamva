namespace Backend.Models.Admin;

public sealed class AdminDashboardResponse
{
    public required decimal TotalRevenue { get; init; }
    public required decimal RevenueChangePercent { get; init; }
    public required int TotalBookings { get; init; }
    public required decimal BookingsChangePercent { get; init; }
    public required int ActiveTutors { get; init; }
    public required int ActiveStudents { get; init; }
    public required int UpcomingBookings { get; init; }
    public required int CompletedBookings { get; init; }
    public required int CancelledBookings { get; init; }
    public required IReadOnlyList<RecentBookingDto> RecentBookings { get; init; }
}
