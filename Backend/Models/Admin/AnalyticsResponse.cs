namespace Backend.Models.Admin;

public sealed class MonthlyPointDto
{
    public required string Label { get; init; }
    public required decimal Value { get; init; }
}

public sealed class AnalyticsResponse
{
    public required IReadOnlyList<MonthlyPointDto> RevenueByMonth { get; init; }
    public required IReadOnlyList<MonthlyPointDto> BookingsByMonth { get; init; }
    public required IReadOnlyList<MonthlyPointDto> CancellationsByMonth { get; init; }
}
