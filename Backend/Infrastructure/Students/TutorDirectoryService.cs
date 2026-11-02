using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Tutors;
using Backend.Models.Student;
using Backend.Models.Tutor;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Students;

public sealed class TutorSearchQuery
{
    public Guid? SubjectId { get; init; }
    public decimal? MaxRate { get; init; }
    public decimal? MinRating { get; init; }
    public string? Search { get; init; }
    public string? Sort { get; init; }
}

public interface ITutorDirectoryService
{
    Task<IReadOnlyList<SubjectRefDto>> GetSubjectsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TutorCardDto>> SearchAsync(TutorSearchQuery query, CancellationToken ct = default);

    Task<TutorPublicProfileDto?> GetProfileAsync(Guid tutorId, CancellationToken ct = default);

    Task<SlotsResponse?> GetSlotsAsync(Guid tutorId, Guid studentId, DateOnly? from, int days, int durationMinutes, CancellationToken ct = default);
}

public class TutorDirectoryService : ITutorDirectoryService
{
    public const int SlotStepMinutes = 30;
    public const int MinimumLeadHours = 2;
    public const int MaximumDaysAhead = 60;
    public static readonly int[] AllowedDurations = [30, 60, 90, 120];

    private readonly AppDbContext _db;

    public TutorDirectoryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SubjectRefDto>> GetSubjectsAsync(CancellationToken ct = default) =>
        await _db.Subjects.OrderBy(s => s.Name).Select(s => new SubjectRefDto { SubjectId = s.Id, Name = s.Name }).ToListAsync(ct);

    public async Task<IReadOnlyList<TutorCardDto>> SearchAsync(TutorSearchQuery query, CancellationToken ct = default)
    {
        var profiles = await BookableProfiles().ToListAsync(ct);
        var stats = await StatsAsync(profiles.Select(p => p.UserId).ToList(), ct);

        var cards = profiles.Select(p =>
        {
            stats.TryGetValue(p.UserId, out var s);
            return new TutorCardDto
            {
                TutorId = p.UserId,
                FullName = p.User.FullName,
                Headline = Headline(p.Bio),
                HourlyRate = p.HourlyRate,
                YearsExperience = p.YearsExperience,
                Subjects = p.Subjects.OrderBy(x => x.Subject.Name).Select(x => new SubjectRefDto { SubjectId = x.SubjectId, Name = x.Subject.Name }).ToList(),
                AverageRating = s.Average,
                ReviewCount = s.Reviews,
                CompletedLessons = s.Completed,
                WeeklyHours = (int)Math.Round(p.Availability.Sum(a => a.EndMinute - a.StartMinute) / 60.0),
            };
        });

        var search = query.Search?.Trim();
        cards = cards.Where(c =>
            (query.SubjectId is null || c.Subjects.Any(s => s.SubjectId == query.SubjectId))
            && (query.MaxRate is null || c.HourlyRate <= query.MaxRate)
            && (query.MinRating is null || (c.AverageRating ?? 0) >= query.MinRating)
            && (string.IsNullOrEmpty(search)
                || c.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || c.Subjects.Any(s => s.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                || (c.Headline ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase)));

        cards = query.Sort switch
        {
            "priceLow" => cards.OrderBy(c => c.HourlyRate).ThenByDescending(c => c.AverageRating ?? 0),
            "priceHigh" => cards.OrderByDescending(c => c.HourlyRate),
            "rating" => cards.OrderByDescending(c => c.AverageRating ?? 0).ThenByDescending(c => c.ReviewCount),
            "experience" => cards.OrderByDescending(c => c.YearsExperience),
            _ => cards
                .OrderByDescending(c => c.WeeklyHours > 0)
                .ThenByDescending(c => (c.AverageRating ?? 0) * (decimal)Math.Log(c.ReviewCount + 2))
                .ThenByDescending(c => c.CompletedLessons),
        };

        return cards.ToList();
    }

    public async Task<TutorPublicProfileDto?> GetProfileAsync(Guid tutorId, CancellationToken ct = default)
    {
        var profile = await BookableProfiles().FirstOrDefaultAsync(p => p.UserId == tutorId, ct);
        if (profile is null)
        {
            return null;
        }

        var stats = await StatsAsync([tutorId], ct);
        stats.TryGetValue(tutorId, out var s);

        var reviews = await (
            from r in _db.LessonReviews
            join student in _db.Users on r.StudentUserId equals student.Id
            where r.TutorUserId == tutorId
            orderby r.CreatedAt descending
            select new { r.Id, student.FullName, Subject = r.Booking.Subject.Name, r.Rating, r.Comment, r.CreatedAt, LessonDate = r.Booking.ScheduledAt }
        ).Take(10).ToListAsync(ct);

        return new TutorPublicProfileDto
        {
            TutorId = profile.UserId,
            FullName = profile.User.FullName,
            Bio = profile.Bio,
            HourlyRate = profile.HourlyRate,
            YearsExperience = profile.YearsExperience,
            Subjects = profile.Subjects.OrderBy(x => x.Subject.Name).Select(x => new SubjectRefDto { SubjectId = x.SubjectId, Name = x.Subject.Name }).ToList(),
            AverageRating = s.Average,
            ReviewCount = s.Reviews,
            CompletedLessons = s.Completed,
            Availability = profile.Availability
                .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartMinute)
                .Select(a => new AvailabilitySlotDto { DayOfWeek = (int)a.DayOfWeek, StartMinute = a.StartMinute, EndMinute = a.EndMinute })
                .ToList(),
            Reviews = reviews.Select(r => new ReviewDto
            {
                ReviewId = r.Id,
                StudentName = ShortName(r.FullName),
                SubjectName = r.Subject,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                LessonDate = r.LessonDate,
            }).ToList(),
        };
    }

    public async Task<SlotsResponse?> GetSlotsAsync(
        Guid tutorId, Guid studentId, DateOnly? from, int days, int durationMinutes, CancellationToken ct = default)
    {
        if (!AllowedDurations.Contains(durationMinutes))
        {
            durationMinutes = 60;
        }

        var profile = await BookableProfiles().FirstOrDefaultAsync(p => p.UserId == tutorId, ct);
        if (profile is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var earliest = now.AddHours(MinimumLeadHours);
        var localToday = DateOnly.FromDateTime(TutorRules.ToLocal(now));
        var lastDay = localToday.AddDays(MaximumDaysAhead);
        var startDay = from is null || from < localToday ? localToday : from.Value > lastDay ? lastDay : from.Value;
        days = Math.Clamp(days, 1, 28);

        var rangeStart = TutorRules.LocalToUtc(startDay.ToDateTime(TimeOnly.MinValue));
        var rangeEnd = TutorRules.LocalToUtc(startDay.AddDays(days).ToDateTime(TimeOnly.MinValue));
        var busy = await BusyAsync(tutorId, studentId, rangeStart, rangeEnd, ct);

        var result = new List<SlotDayDto>();
        for (var i = 0; i < days; i++)
        {
            var day = startDay.AddDays(i);
            if (day > lastDay)
            {
                break;
            }

            var starts = new List<DateTime>();
            foreach (var window in profile.Availability.Where(a => a.DayOfWeek == day.DayOfWeek).OrderBy(a => a.StartMinute))
            {
                for (var minute = window.StartMinute; minute + durationMinutes <= window.EndMinute; minute += SlotStepMinutes)
                {
                    var startUtc = TutorRules.LocalToUtc(day.ToDateTime(TimeOnly.MinValue).AddMinutes(minute));
                    var endUtc = startUtc.AddMinutes(durationMinutes);
                    if (startUtc < earliest || busy.Any(b => b.Start < endUtc && startUtc < b.End))
                    {
                        continue;
                    }
                    starts.Add(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc));
                }
            }

            result.Add(new SlotDayDto { Date = day.ToString("yyyy-MM-dd"), Starts = starts });
        }

        return new SlotsResponse { TimeZone = "Africa/Johannesburg", DurationMinutes = durationMinutes, Days = result };
    }

    internal async Task<List<(DateTime Start, DateTime End)>> BusyAsync(
        Guid tutorId, Guid studentId, DateTime from, DateTime to, CancellationToken ct)
    {
        var windowStart = from.AddHours(-4);
        var rows = await _db.Bookings
            .Where(b => (b.TutorUserId == tutorId || b.StudentUserId == studentId)
                && b.Status != BookingStatus.Cancelled
                && b.ScheduledAt >= windowStart && b.ScheduledAt < to)
            .Select(b => new { b.ScheduledAt, b.DurationMinutes })
            .ToListAsync(ct);

        return rows.Select(r => (r.ScheduledAt, r.ScheduledAt.AddMinutes(r.DurationMinutes))).ToList();
    }

    internal IQueryable<TutorProfile> BookableProfiles() =>
        _db.TutorProfiles
            .Include(t => t.User)
            .Include(t => t.Subjects).ThenInclude(s => s.Subject)
            .Include(t => t.Availability)
            .Where(t => t.VerificationStatus == VerificationStatus.Approved && t.User.IsActive && t.HourlyRate > 0);

    private async Task<Dictionary<Guid, (decimal? Average, int Reviews, int Completed)>> StatsAsync(List<Guid> tutorIds, CancellationToken ct)
    {
        var ratings = await _db.LessonReviews
            .Where(r => tutorIds.Contains(r.TutorUserId))
            .GroupBy(r => r.TutorUserId)
            .Select(g => new { TutorId = g.Key, Average = g.Average(r => (double)r.Rating), Count = g.Count() })
            .ToListAsync(ct);

        var completed = await _db.Bookings
            .Where(b => tutorIds.Contains(b.TutorUserId) && b.Status == BookingStatus.Completed)
            .GroupBy(b => b.TutorUserId)
            .Select(g => new { TutorId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return tutorIds.ToDictionary(id => id, id =>
        {
            var r = ratings.FirstOrDefault(x => x.TutorId == id);
            var c = completed.FirstOrDefault(x => x.TutorId == id);
            return (r is null ? (decimal?)null : Math.Round((decimal)r.Average, 1), r?.Count ?? 0, c?.Count ?? 0);
        });
    }

    private static string? Headline(string? bio)
    {
        if (string.IsNullOrWhiteSpace(bio))
        {
            return null;
        }
        var text = bio.Trim().ReplaceLineEndings(" ");
        return text.Length <= 140 ? text : text[..137].TrimEnd() + "…";
    }

    private static string ShortName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length < 2 ? fullName : $"{parts[0]} {parts[^1][0]}.";
    }
}
