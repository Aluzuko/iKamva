using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Admin;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var lastMonth = now.AddMonths(-1);

        var totalRevenue = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid)
            .SumAsync(i => (decimal?)i.AmountTotal, ct) ?? 0m;

        var currentMonthRevenue = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.Year == now.Year && i.Month == now.Month)
            .SumAsync(i => (decimal?)i.AmountTotal, ct) ?? 0m;

        var previousMonthRevenue = await _db.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid && i.Year == lastMonth.Year && i.Month == lastMonth.Month)
            .SumAsync(i => (decimal?)i.AmountTotal, ct) ?? 0m;

        var totalBookings = await _db.Bookings.CountAsync(ct);

        var currentMonthBookings = await _db.Bookings.CountAsync(
            b => b.ScheduledAt.Year == now.Year && b.ScheduledAt.Month == now.Month, ct);

        var previousMonthBookings = await _db.Bookings.CountAsync(
            b => b.ScheduledAt.Year == lastMonth.Year && b.ScheduledAt.Month == lastMonth.Month, ct);

        var activeTutors = await _db.Users.CountAsync(u => u.Role == UserRole.Tutor, ct);
        var activeStudents = await _db.Users.CountAsync(u => u.Role == UserRole.Student, ct);

        var upcoming = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Upcoming, ct);
        var completed = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Completed, ct);
        var cancelled = await _db.Bookings.CountAsync(b => b.Status == BookingStatus.Cancelled, ct);

        var recentBookings = await (
            from b in _db.Bookings
            join student in _db.Users on b.StudentUserId equals student.Id
            join tutor in _db.Users on b.TutorUserId equals tutor.Id
            orderby b.ScheduledAt descending
            select new RecentBookingDto
            {
                BookingId = b.Id,
                SubjectName = b.Subject.Name,
                StudentName = student.FullName,
                TutorName = tutor.FullName,
                ScheduledAt = b.ScheduledAt,
                Price = b.PriceAmount,
                Status = b.Status,
            }
        ).Take(8).ToListAsync(ct);

        return new AdminDashboardResponse
        {
            TotalRevenue = totalRevenue,
            RevenueChangePercent = PercentChange(currentMonthRevenue, previousMonthRevenue),
            TotalBookings = totalBookings,
            BookingsChangePercent = PercentChange(currentMonthBookings, previousMonthBookings),
            ActiveTutors = activeTutors,
            ActiveStudents = activeStudents,
            UpcomingBookings = upcoming,
            CompletedBookings = completed,
            CancelledBookings = cancelled,
            RecentBookings = recentBookings,
        };
    }

    private static decimal PercentChange(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0m : 100m;
        }

        return Math.Round((current - previous) / previous * 100, 1);
    }
}
