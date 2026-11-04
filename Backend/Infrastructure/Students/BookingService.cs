using System.Data;
using System.Data.Common;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Tutors;
using Backend.Models.Student;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Students;

public interface IBookingService
{
    Task<ServiceResult<BookingCreatedDto>> CreateAsync(Guid studentId, CreateBookingRequest request, CancellationToken ct = default);
}

public class BookingService : IBookingService
{
    private const string SlotTaken = "That time was just taken. Pick another slot.";

    private readonly AppDbContext _db;
    private readonly TutorDirectoryService _directory;
    private readonly IInvoiceService _invoices;

    public BookingService(AppDbContext db, TutorDirectoryService directory, IInvoiceService invoices)
    {
        _db = db;
        _directory = directory;
        _invoices = invoices;
    }

    public async Task<ServiceResult<BookingCreatedDto>> CreateAsync(Guid studentId, CreateBookingRequest request, CancellationToken ct = default)
    {
        if (!TutorDirectoryService.AllowedDurations.Contains(request.DurationMinutes))
        {
            return Fail(ServiceResult.Invalid("Lessons can be 30, 60, 90 or 120 minutes long."));
        }

        var start = request.StartUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(request.StartUtc, DateTimeKind.Utc)
            : request.StartUtc.ToUniversalTime();
        if (start.Second != 0 || start.Millisecond != 0 || start.Minute % 15 != 0)
        {
            return Fail(ServiceResult.Invalid("Choose one of the times offered."));
        }

        var now = DateTime.UtcNow;
        if (start < now.AddHours(TutorDirectoryService.MinimumLeadHours))
        {
            return Fail(ServiceResult.Invalid($"Lessons must be booked at least {TutorDirectoryService.MinimumLeadHours} hours ahead."));
        }
        if (start > now.AddDays(TutorDirectoryService.MaximumDaysAhead))
        {
            return Fail(ServiceResult.Invalid($"Lessons can be booked up to {TutorDirectoryService.MaximumDaysAhead} days ahead."));
        }

        var student = await _db.Users.FirstOrDefaultAsync(u => u.Id == studentId && u.Role == UserRole.Student && u.IsActive, ct);
        if (student is null)
        {
            return Fail(ServiceResult.NotFound());
        }

        var tutor = await _directory.BookableProfiles().FirstOrDefaultAsync(p => p.UserId == request.TutorId, ct);
        if (tutor is null)
        {
            return Fail(ServiceResult.NotFound("This tutor isn't taking bookings."));
        }

        var subject = tutor.Subjects.FirstOrDefault(s => s.SubjectId == request.SubjectId)?.Subject;
        if (subject is null)
        {
            return Fail(ServiceResult.Invalid("This tutor doesn't teach that subject."));
        }

        var local = TutorRules.ToLocal(start);
        var minute = local.Hour * 60 + local.Minute;
        var fits = tutor.Availability.Any(a =>
            a.DayOfWeek == local.DayOfWeek && a.StartMinute <= minute && minute + request.DurationMinutes <= a.EndMinute);
        if (!fits)
        {
            return Fail(ServiceResult.Conflict("That time is outside the tutor’s available hours."));
        }

        var end = start.AddMinutes(request.DurationMinutes);
        var booking = new Booking
        {
            StudentUserId = studentId,
            TutorUserId = tutor.UserId,
            SubjectId = subject.Id,
            ScheduledAt = start,
            DurationMinutes = request.DurationMinutes,
            PriceAmount = Math.Round(tutor.HourlyRate * request.DurationMinutes / 60m, 2, MidpointRounding.AwayFromZero),
            CommissionPercent = subject.CommissionPercent,
            Status = BookingStatus.Upcoming,
        };

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var busy = await _directory.BusyAsync(tutor.UserId, studentId, start, end, ct);
            if (busy.Any(b => b.Start < end && start < b.End))
            {
                return Fail(ServiceResult.Conflict(SlotTaken));
            }

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync(ct);

            var invoiceResult = await _invoices.CreateForBookingAsync(studentId, booking.Id, ct);
            if (invoiceResult.Outcome != ServiceOutcome.Ok || invoiceResult.Value is null)
            {
                return Fail(new ServiceResult(invoiceResult.Outcome, invoiceResult.Message));
            }

            await transaction.CommitAsync(ct);

            return ServiceResult<BookingCreatedDto>.Ok(new BookingCreatedDto
            {
                BookingId = booking.Id,
                InvoiceId = invoiceResult.Value.Invoice.InvoiceId,
                ScheduledAt = booking.ScheduledAt,
                Price = booking.PriceAmount,
                PaymentRequired = invoiceResult.Value.Invoice.PaymentRequired,
            });
        }
        catch (Exception ex) when (IsConcurrencyConflict(ex))
        {
            return Fail(ServiceResult.Conflict(SlotTaken));
        }
    }

    private static ServiceResult<BookingCreatedDto> Fail(ServiceResult result) => ServiceResult<BookingCreatedDto>.From(result);

    private static bool IsConcurrencyConflict(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is DbException { SqlState: "40001" or "40P01" })
            {
                return true;
            }
        }
        return false;
    }
}
