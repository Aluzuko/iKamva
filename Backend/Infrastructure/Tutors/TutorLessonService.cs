using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Models.Tutor;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Tutors;

public interface ITutorLessonService
{
    Task<TutorDashboardResponse> GetDashboardAsync(Guid tutorId, CancellationToken ct = default);

    Task<IReadOnlyList<TutorLessonDto>> GetLessonsAsync(Guid tutorId, CancellationToken ct = default);

    Task<ServiceResult> CancelAsync(Guid tutorId, Guid bookingId, string reason, CancellationToken ct = default);

    Task<ServiceResult> CompleteAsync(Guid tutorId, Guid bookingId, CancellationToken ct = default);

    Task<ServiceResult> UpdateNotesAsync(Guid tutorId, Guid bookingId, UpdateLessonNotesRequest request, CancellationToken ct = default);

    Task<FeedbackResponse> GetFeedbackAsync(Guid tutorId, CancellationToken ct = default);
}

public class TutorLessonService : ITutorLessonService
{
    private readonly AppDbContext _db;

    public TutorLessonService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TutorDashboardResponse> GetDashboardAsync(Guid tutorId, CancellationToken ct = default)
    {
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(t => t.UserId == tutorId, ct);
        var lessons = await GetLessonsAsync(tutorId, ct);

        var now = DateTime.UtcNow;
        var localNow = TutorRules.ToLocal(now);
        var todayStart = TutorRules.LocalToUtc(localNow.Date);
        var tomorrowStart = TutorRules.LocalToUtc(localNow.Date.AddDays(1));
        var monthStart = TutorRules.LocalToUtc(new DateTime(localNow.Year, localNow.Month, 1));
        var previousMonthStart = TutorRules.LocalToUtc(new DateTime(localNow.Year, localNow.Month, 1).AddMonths(-1));

        var completed = lessons.Where(l => l.Status == BookingStatus.Completed).ToList();
        var thisMonth = completed.Where(l => l.ScheduledAt >= monthStart).ToList();
        var previousMonth = completed.Where(l => l.ScheduledAt >= previousMonthStart && l.ScheduledAt < monthStart).ToList();
        var earningsThisMonth = thisMonth.Sum(l => l.Earning);

        var ratings = await _db.LessonReviews.Where(r => r.TutorUserId == tutorId).Select(r => r.Rating).ToListAsync(ct);

        return new TutorDashboardResponse
        {
            VerificationState = TutorRules.VerificationState(profile),
            RejectionReason = profile?.VerificationStatus == VerificationStatus.Rejected ? profile.RejectionReason : null,
            TodayLessons = lessons
                .Where(l => l.Status != BookingStatus.Cancelled && l.ScheduledAt >= todayStart && l.ScheduledAt < tomorrowStart)
                .OrderBy(l => l.ScheduledAt)
                .ToList(),
            NextLesson = lessons
                .Where(l => l.Status == BookingStatus.Upcoming && l.ScheduledAt >= now)
                .OrderBy(l => l.ScheduledAt)
                .FirstOrDefault(),
            UpcomingThisWeek = lessons.Count(l => l.Status == BookingStatus.Upcoming && l.ScheduledAt >= now && l.ScheduledAt < now.AddDays(7)),
            CompletedThisMonth = thisMonth.Count,
            EarningsThisMonth = earningsThisMonth,
            EarningsChangePercent = TutorRules.ChangePercent(earningsThisMonth, previousMonth.Sum(l => l.Earning)),
            AverageRating = ratings.Count == 0 ? null : Math.Round((decimal)ratings.Average(), 1),
            ReviewCount = ratings.Count,
            LessonsNeedingNotes = completed.Count(l => string.IsNullOrWhiteSpace(l.Notes)),
            LessonsNeedingCompletion = lessons.Count(l => l.NeedsCompletion),
            HasAvailability = await _db.TutorAvailabilitySlots.AnyAsync(s => s.TutorUserId == tutorId, ct),
        };
    }

    public async Task<IReadOnlyList<TutorLessonDto>> GetLessonsAsync(Guid tutorId, CancellationToken ct = default)
    {
        var rows = await (
            from b in _db.Bookings
            join s in _db.Users on b.StudentUserId equals s.Id
            where b.TutorUserId == tutorId
            orderby b.ScheduledAt descending
            select new { Booking = b, StudentName = s.FullName, SubjectName = b.Subject.Name }
        ).ToListAsync(ct);

        var bookingIds = rows.Select(r => r.Booking.Id).ToList();
        var ratings = await _db.LessonReviews
            .Where(r => bookingIds.Contains(r.BookingId))
            .ToDictionaryAsync(r => r.BookingId, r => r.Rating, ct);

        var now = DateTime.UtcNow;
        return rows.Select(r => Map(r.Booking, r.StudentName, r.SubjectName, now, ratings.TryGetValue(r.Booking.Id, out var rating) ? rating : null)).ToList();
    }

    public async Task<ServiceResult> CancelAsync(Guid tutorId, Guid bookingId, string reason, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.TutorUserId == tutorId, ct);
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
            return ServiceResult.Conflict("This lesson has already started, so it can't be cancelled. Mark it as completed or raise it with support.");
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = reason.Trim();
        booking.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CompleteAsync(Guid tutorId, Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.TutorUserId == tutorId, ct);
        if (booking is null)
        {
            return ServiceResult.NotFound();
        }
        if (booking.Status != BookingStatus.Upcoming)
        {
            return ServiceResult.Conflict("Only upcoming lessons can be marked as completed.");
        }
        if (booking.ScheduledAt > DateTime.UtcNow)
        {
            return ServiceResult.Conflict("You can mark a lesson as completed once it has started.");
        }

        booking.Status = BookingStatus.Completed;
        booking.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateNotesAsync(
        Guid tutorId, Guid bookingId, UpdateLessonNotesRequest request, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.TutorUserId == tutorId, ct);
        if (booking is null)
        {
            return ServiceResult.NotFound();
        }
        if (booking.Status != BookingStatus.Completed)
        {
            return ServiceResult.Conflict("Notes can only be added to completed lessons.");
        }

        var url = string.IsNullOrWhiteSpace(request.RecordingUrl) ? null : request.RecordingUrl.Trim();
        if (url is not null && !(Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)))
        {
            return ServiceResult.Invalid("The recording link must start with https://");
        }

        booking.Notes = request.Notes.Trim();
        booking.RecordingUrl = url;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<FeedbackResponse> GetFeedbackAsync(Guid tutorId, CancellationToken ct = default)
    {
        var reviews = await (
            from r in _db.LessonReviews
            join s in _db.Users on r.StudentUserId equals s.Id
            where r.TutorUserId == tutorId
            orderby r.CreatedAt descending
            select new ReviewDto
            {
                ReviewId = r.Id,
                StudentName = s.FullName,
                SubjectName = r.Booking.Subject.Name,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                LessonDate = r.Booking.ScheduledAt,
            }
        ).ToListAsync(ct);

        var distribution = Enumerable.Range(1, 5).Select(star => reviews.Count(r => r.Rating == star)).ToList();

        return new FeedbackResponse
        {
            AverageRating = reviews.Count == 0 ? null : Math.Round((decimal)reviews.Average(r => r.Rating), 1),
            ReviewCount = reviews.Count,
            Distribution = distribution,
            Reviews = reviews,
        };
    }

    private static TutorLessonDto Map(Booking b, string studentName, string subjectName, DateTime now, int? rating) => new()
    {
        BookingId = b.Id,
        StudentName = studentName,
        SubjectName = subjectName,
        ScheduledAt = b.ScheduledAt,
        DurationMinutes = b.DurationMinutes,
        Price = b.PriceAmount,
        Earning = TutorRules.Earning(b.PriceAmount, b.CommissionPercent),
        Status = b.Status,
        Notes = b.Notes,
        RecordingUrl = b.RecordingUrl,
        CancellationReason = b.CancellationReason,
        NeedsCompletion = b.Status == BookingStatus.Upcoming && b.ScheduledAt.AddMinutes(b.DurationMinutes) <= now,
        Rating = rating,
    };
}
