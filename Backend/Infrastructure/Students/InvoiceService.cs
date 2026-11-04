using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Tutors;
using Backend.Models.Student;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Students;

public sealed record InvoiceCreationResult(StudentInvoiceResponse Invoice, bool Created);

public interface IInvoiceService
{
    Task<ServiceResult<InvoiceCreationResult>> CreateForBookingAsync(
        Guid studentId, Guid bookingId, CancellationToken ct = default);

    Task<StudentInvoiceResponse?> GetForStudentAsync(Guid studentId, Guid invoiceId, CancellationToken ct = default);
}

public sealed class InvoiceService : IInvoiceService
{
    private static readonly string[] MonthNames =
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    private readonly AppDbContext _db;

    public InvoiceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<InvoiceCreationResult>> CreateForBookingAsync(
        Guid studentId, Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(
            b => b.Id == bookingId && b.StudentUserId == studentId, ct);
        if (booking is null)
        {
            return ServiceResult<InvoiceCreationResult>.From(ServiceResult.NotFound("Booking not found."));
        }
        if (booking.Status == BookingStatus.Cancelled)
        {
            return ServiceResult<InvoiceCreationResult>.From(
                ServiceResult.Conflict("A cancelled booking cannot be invoiced."));
        }

        var existing = await _db.Invoices.FirstOrDefaultAsync(i => i.BookingId == bookingId, ct);
        if (existing is not null)
        {
            return ServiceResult<InvoiceCreationResult>.Ok(new InvoiceCreationResult(ToResponse(existing), false));
        }

        var parentUserId = await _db.ParentStudentLinks
            .Where(l => l.StudentUserId == studentId && l.Status == LinkStatus.Active)
            .OrderBy(l => l.CreatedAt)
            .ThenBy(l => l.ParentUserId)
            .Select(l => (Guid?)l.ParentUserId)
            .FirstOrDefaultAsync(ct);

        var localScheduledAt = TutorRules.ToLocal(booking.ScheduledAt);
        var invoice = new Invoice
        {
            ParentUserId = parentUserId,
            StudentUserId = studentId,
            BookingId = bookingId,
            Year = localScheduledAt.Year,
            Month = localScheduledAt.Month,
            LessonCount = 1,
            AmountTotal = booking.PriceAmount,
            Status = InvoiceStatus.Pending,
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);

        return ServiceResult<InvoiceCreationResult>.Ok(new InvoiceCreationResult(ToResponse(invoice), true));
    }

    public async Task<StudentInvoiceResponse?> GetForStudentAsync(
        Guid studentId, Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(
            i => i.Id == invoiceId && i.StudentUserId == studentId && i.BookingId != null, ct);
        return invoice is null ? null : ToResponse(invoice);
    }

    private static StudentInvoiceResponse ToResponse(Invoice invoice) => new(
        invoice.Id,
        invoice.BookingId!.Value,
        $"{MonthNames[invoice.Month - 1]} {invoice.Year} · {invoice.LessonCount} {(invoice.LessonCount == 1 ? "lesson" : "lessons")}",
        invoice.AmountTotal,
        invoice.Status,
        invoice.IssuedAt,
        invoice.ParentUserId is null);
}
