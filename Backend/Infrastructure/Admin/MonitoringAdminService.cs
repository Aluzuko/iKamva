using Backend.Infrastructure.Data;
using Backend.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Admin;

public interface IMonitoringAdminService
{
    Task<IReadOnlyList<AdminBookingDto>> GetBookingsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<AdminInvoiceDto>> GetPaymentsAsync(CancellationToken ct = default);
}

public class MonitoringAdminService : IMonitoringAdminService
{
    private readonly AppDbContext _db;

    public MonitoringAdminService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AdminBookingDto>> GetBookingsAsync(CancellationToken ct = default)
    {
        return await (
            from b in _db.Bookings
            join student in _db.Users on b.StudentUserId equals student.Id
            join tutor in _db.Users on b.TutorUserId equals tutor.Id
            orderby b.ScheduledAt descending
            select new AdminBookingDto
            {
                BookingId = b.Id,
                SubjectName = b.Subject.Name,
                StudentName = student.FullName,
                TutorName = tutor.FullName,
                ScheduledAt = b.ScheduledAt,
                Price = b.PriceAmount,
                Status = b.Status,
            }
        ).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AdminInvoiceDto>> GetPaymentsAsync(CancellationToken ct = default)
    {
        return await (
            from i in _db.Invoices
            join parent in _db.Users on i.ParentUserId equals parent.Id
            join student in _db.Users on i.StudentUserId equals student.Id
            orderby i.Year descending, i.Month descending
            select new AdminInvoiceDto
            {
                InvoiceId = i.Id,
                ParentName = parent.FullName,
                StudentName = student.FullName,
                PeriodDescription = MonthName(i.Month) + " " + i.Year,
                Amount = i.AmountTotal,
                Status = i.Status,
            }
        ).ToListAsync(ct);
    }

    private static string MonthName(int month) =>
        System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
}
