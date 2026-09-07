using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Admin;

public interface IAnalyticsAdminService
{
    Task<AnalyticsResponse> GetAnalyticsAsync(CancellationToken ct = default);
}

public class AnalyticsAdminService : IAnalyticsAdminService
{
    private readonly AppDbContext _db;

    public AnalyticsAdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AnalyticsResponse> GetAnalyticsAsync(CancellationToken ct = default)
    {
        var months = LastSixMonths();

        var revenue = new List<MonthlyPointDto>();
        var bookings = new List<MonthlyPointDto>();
        var cancellations = new List<MonthlyPointDto>();

        foreach (var (year, month, label) in months)
        {
            var monthRevenue = await _db.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid && i.Year == year && i.Month == month)
                .SumAsync(i => (decimal?)i.AmountTotal, ct) ?? 0m;

            var monthBookings = await _db.Bookings.CountAsync(
                b => b.ScheduledAt.Year == year && b.ScheduledAt.Month == month, ct);

            var monthCancellations = await _db.Bookings.CountAsync(
                b => b.Status == BookingStatus.Cancelled && b.ScheduledAt.Year == year && b.ScheduledAt.Month == month, ct);

            revenue.Add(new MonthlyPointDto { Label = label, Value = monthRevenue });
            bookings.Add(new MonthlyPointDto { Label = label, Value = monthBookings });
            cancellations.Add(new MonthlyPointDto { Label = label, Value = monthCancellations });
        }

        return new AnalyticsResponse
        {
            RevenueByMonth = revenue,
            BookingsByMonth = bookings,
            CancellationsByMonth = cancellations,
        };
    }

    private static List<(int Year, int Month, string Label)> LastSixMonths()
    {
        var now = DateTime.UtcNow;
        var result = new List<(int, int, string)>();

        for (var i = 5; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            var label = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedMonthName(date.Month);
            result.Add((date.Year, date.Month, label));
        }

        return result;
    }
}
