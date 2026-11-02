using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Tutors;
using Backend.Models.Student;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Students;

public interface IStudentService
{
    Task<StudentDashboardResponse> GetDashboardAsync(Guid studentId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentLessonDto>> GetLessonsAsync(Guid studentId, CancellationToken ct = default);

    Task<ServiceResult> CancelAsync(Guid studentId, Guid bookingId, string reason, CancellationToken ct = default);

    Task<ServiceResult> ReviewAsync(Guid studentId, Guid bookingId, CreateReviewRequest request, CancellationToken ct = default);

    Task<StudentBillingResponse> GetBillingAsync(Guid studentId, CancellationToken ct = default);
}

public class StudentService : IStudentService
{
    private static readonly string[] MonthNames =
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    private readonly AppDbContext _db;

    public StudentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<StudentDashboardResponse> GetDashboardAsync(Guid studentId, CancellationToken ct = default)
    {
        var lessons = await GetLessonsAsync(studentId, ct);
        var now = DateTime.UtcNow;
        var localNow = TutorRules.ToLocal(now);
        var monthStart = TutorRules.LocalToUtc(new DateTime(localNow.Year, localNow.Month, 1));

        var upcoming = lessons
            .Where(l => l.Status == BookingStatus.Upcoming && l.ScheduledAt.AddMinutes(l.DurationMinutes) > now)
            .OrderBy(l => l.ScheduledAt)
            .ToList();
        var completed = lessons.Where(l => l.Status == BookingStatus.Completed).ToList();

        return new StudentDashboardResponse
        {
            NextLesson = upcoming.FirstOrDefault(),
            UpcomingLessons = upcoming.Take(5).ToList(),
            UpcomingCount = upcoming.Count,
            CompletedCount = completed.Count,
            HoursThisMonth = Math.Round(completed.Where(l => l.ScheduledAt >= monthStart).Sum(l => l.DurationMinutes) / 60m, 1),
            LessonsToReview = completed.Count(l => l.CanReview),
            Subjects = completed
                .GroupBy(l => l.SubjectName)
                .Select(g => new SubjectProgressDto
                {
                    SubjectName = g.Key,
                    CompletedLessons = g.Count(),
                    Hours = Math.Round(g.Sum(l => l.DurationMinutes) / 60m, 1),
                    LastLessonAt = g.Max(l => l.ScheduledAt),
                })
                .OrderByDescending(s => s.CompletedLessons)
                .ToList(),
            LinkCode = await LinkCodeAsync(studentId, ct),
            Parents = await ParentsAsync(studentId, ct),
        };
    }

    public async Task<IReadOnlyList<StudentLessonDto>> GetLessonsAsync(Guid studentId, CancellationToken ct = default)
    {
        var rows = await (
            from b in _db.Bookings
            join t in _db.Users on b.TutorUserId equals t.Id
            where b.StudentUserId == studentId
            orderby b.ScheduledAt descending
            select new { Booking = b, TutorName = t.FullName, SubjectName = b.Subject.Name }
        ).ToListAsync(ct);

        var ids = rows.Select(r => r.Booking.Id).ToList();
        var reviews = await _db.LessonReviews
            .Where(r => ids.Contains(r.BookingId) && r.StudentUserId == studentId)
            .ToDictionaryAsync(r => r.BookingId, ct);

        var now = DateTime.UtcNow;
        return rows.Select(r =>
        {
            var b = r.Booking;
            reviews.TryGetValue(b.Id, out var review);
            return new StudentLessonDto
            {
                BookingId = b.Id,
                TutorId = b.TutorUserId,
                TutorName = r.TutorName,
                SubjectName = r.SubjectName,
                ScheduledAt = b.ScheduledAt,
                DurationMinutes = b.DurationMinutes,
                Price = b.PriceAmount,
                Status = b.Status,
                Notes = b.Notes,
                RecordingUrl = b.RecordingUrl,
                CancellationReason = b.CancellationReason,
                CanCancel = b.Status == BookingStatus.Upcoming && b.ScheduledAt > now,
                CanReview = b.Status == BookingStatus.Completed && review is null,
                MyRating = review?.Rating,
                MyComment = review?.Comment,
            };
        }).ToList();
    }

    public async Task<ServiceResult> CancelAsync(Guid studentId, Guid bookingId, string reason, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.StudentUserId == studentId, ct);
        if (booking is null)
        {
            return ServiceResult.NotFound();
        }
        if (booking.Status != BookingStatus.Upcoming)
        {
            return ServiceResult.Conflict("Only upcoming lessons can be cancelled.");
        }
        if (booking.ScheduledAt <= DateTime.UtcNow)
        {
            return ServiceResult.Conflict("This lesson has already started, so it can’t be cancelled.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = reason.Trim();
        booking.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ReviewAsync(Guid studentId, Guid bookingId, CreateReviewRequest request, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.StudentUserId == studentId, ct);
        if (booking is null)
        {
            return ServiceResult.NotFound();
        }
        if (booking.Status != BookingStatus.Completed)
        {
            return ServiceResult.Conflict("You can rate a lesson once it’s completed.");
        }
        if (await _db.LessonReviews.AnyAsync(r => r.BookingId == bookingId, ct))
        {
            return ServiceResult.Conflict("You’ve already rated this lesson.");
        }

        _db.LessonReviews.Add(new LessonReview
        {
            BookingId = bookingId,
            StudentUserId = studentId,
            TutorUserId = booking.TutorUserId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Conflict("You’ve already rated this lesson.");
        }
        return ServiceResult.Ok();
    }

    public async Task<StudentBillingResponse> GetBillingAsync(Guid studentId, CancellationToken ct = default)
    {
        var parents = await ParentsAsync(studentId, ct);
        var lessons = await GetLessonsAsync(studentId, ct);
        var now = DateTime.UtcNow;
        var localNow = TutorRules.ToLocal(now);
        var monthStart = TutorRules.LocalToUtc(new DateTime(localNow.Year, localNow.Month, 1));
        var monthEnd = TutorRules.LocalToUtc(new DateTime(localNow.Year, localNow.Month, 1).AddMonths(1));

        var invoices = await _db.Invoices
            .Where(i => i.StudentUserId == studentId)
            .OrderByDescending(i => i.Year).ThenByDescending(i => i.Month)
            .ToListAsync(ct);

        var thisMonth = lessons
            .Where(l => l.Status != BookingStatus.Cancelled && l.ScheduledAt >= monthStart && l.ScheduledAt < monthEnd)
            .OrderBy(l => l.ScheduledAt)
            .Select(l => new ChargeDto
            {
                BookingId = l.BookingId,
                ScheduledAt = l.ScheduledAt,
                TutorName = l.TutorName,
                SubjectName = l.SubjectName,
                DurationMinutes = l.DurationMinutes,
                Price = l.Price,
                Status = l.Status,
            })
            .ToList();

        return new StudentBillingResponse
        {
            PaidByParent = parents.Count > 0,
            Parents = parents,
            LinkCode = await LinkCodeAsync(studentId, ct),
            Outstanding = invoices.Where(i => i.Status == InvoiceStatus.Pending).Sum(i => i.AmountTotal),
            ThisMonthTotal = thisMonth.Where(c => c.Status == BookingStatus.Completed).Sum(c => c.Price),
            UpcomingTotal = lessons.Where(l => l.Status == BookingStatus.Upcoming && l.ScheduledAt > now).Sum(l => l.Price),
            ThisMonth = thisMonth,
            Invoices = invoices.Select(i => new StudentInvoiceDto
            {
                InvoiceId = i.Id,
                PeriodDescription = $"{MonthNames[i.Month - 1]} {i.Year} · {i.LessonCount} {(i.LessonCount == 1 ? "lesson" : "lessons")}",
                Amount = i.AmountTotal,
                Status = i.Status,
                PaidAt = i.PaidAt,
            }).ToList(),
        };
    }

    private async Task<string> LinkCodeAsync(Guid studentId, CancellationToken ct) =>
        await _db.StudentProfiles.Where(p => p.UserId == studentId).Select(p => p.LinkCode).FirstOrDefaultAsync(ct) ?? string.Empty;

    private async Task<IReadOnlyList<LinkedParentDto>> ParentsAsync(Guid studentId, CancellationToken ct) =>
        await (
            from l in _db.ParentStudentLinks
            join p in _db.Users on l.ParentUserId equals p.Id
            where l.StudentUserId == studentId && l.Status == LinkStatus.Active
            orderby p.FullName
            select new LinkedParentDto { FullName = p.FullName, Relationship = l.Relationship }
        ).ToListAsync(ct);
}
