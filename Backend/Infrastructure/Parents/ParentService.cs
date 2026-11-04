using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Email;
using Backend.Models.Parent;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Parents;

public class ParentService : IParentService
{
    private readonly AppDbContext _db;
    private readonly IEmailQueue _emails;
    private readonly IEmailTemplates _templates;

    public ParentService(AppDbContext db, IEmailQueue emails, IEmailTemplates templates)
    {
        _db = db;
        _emails = emails;
        _templates = templates;
    }

    public async Task<FamilyDashboardResponse> GetDashboardAsync(Guid parentUserId, CancellationToken ct = default)
    {
        var studentIds = await ActiveStudentIdsAsync(parentUserId, ct);
        var children = await BuildChildSummariesAsync(studentIds, ct);

        var now = DateTime.UtcNow;
        var weekAhead = now.AddDays(7);

        var upcomingThisWeek = await _db.Bookings
            .Where(b => studentIds.Contains(b.StudentUserId)
                && b.Status == BookingStatus.Upcoming
                && b.ScheduledAt >= now && b.ScheduledAt <= weekAhead)
            .CountAsync(ct);

        var (monthlySpend, monthlySpendChange) = await MonthlyAmountWithChangeAsync(studentIds, now, ct);
        var (totalSessions, totalSessionsChange) = await TotalSessionsWithChangeAsync(studentIds, now, ct);

        var upcomingLessons = await UpcomingLessonsAsync(studentIds, 10, ct);
        var outstandingInvoice = await OutstandingInvoiceAsync(parentUserId, ct);

        return new FamilyDashboardResponse
        {
            ChildrenLinked = studentIds.Count,
            UpcomingLessonsThisWeek = upcomingThisWeek,
            MonthlySpend = monthlySpend,
            MonthlySpendChangePercent = monthlySpendChange,
            TotalSessions = totalSessions,
            TotalSessionsChangePercent = totalSessionsChange,
            Children = children,
            UpcomingLessons = upcomingLessons,
            OutstandingInvoice = outstandingInvoice,
        };
    }

    public async Task<IReadOnlyList<ChildSummaryDto>> GetChildrenAsync(Guid parentUserId, CancellationToken ct = default)
    {
        var studentIds = await ActiveStudentIdsAsync(parentUserId, ct);
        return await BuildChildSummariesAsync(studentIds, ct);
    }

    public async Task<StudentDetailResponse?> GetChildDetailAsync(
        Guid parentUserId, Guid studentUserId, CancellationToken ct = default)
    {
        var isLinked = await _db.ParentStudentLinks.AnyAsync(
            l => l.ParentUserId == parentUserId && l.StudentUserId == studentUserId && l.Status == LinkStatus.Active, ct);

        if (!isLinked)
        {
            return null;
        }

        var student = await _db.Users.FirstOrDefaultAsync(u => u.Id == studentUserId, ct);
        if (student is null)
        {
            return null;
        }

        var profile = await _db.StudentProfiles
            .Include(p => p.Subjects).ThenInclude(s => s.Subject)
            .FirstOrDefaultAsync(p => p.UserId == studentUserId, ct);

        var upcomingLessons = await UpcomingLessonsAsync(new[] { studentUserId }, 20, ct);

        var history = await (
            from b in _db.Bookings
            join tutor in _db.Users on b.TutorUserId equals tutor.Id
            where b.StudentUserId == studentUserId && b.Status != BookingStatus.Upcoming
            orderby b.ScheduledAt descending
            select new LessonHistoryItemDto
            {
                BookingId = b.Id,
                SubjectName = b.Subject.Name,
                TutorName = tutor.FullName,
                ScheduledAt = b.ScheduledAt,
                DurationMinutes = b.DurationMinutes,
                Status = b.Status,
                Notes = b.Notes,
                RecordingUrl = b.RecordingUrl,
            }
        ).ToListAsync(ct);

        var upcomingCount = await _db.Bookings.CountAsync(
            b => b.StudentUserId == studentUserId && b.Status == BookingStatus.Upcoming, ct);
        var completedCount = await _db.Bookings.CountAsync(
            b => b.StudentUserId == studentUserId && b.Status == BookingStatus.Completed, ct);

        return new StudentDetailResponse
        {
            StudentId = studentUserId,
            FullName = student.FullName,
            YearLevel = profile?.YearLevel,
            EnrolledSubjects = profile?.Subjects.Select(s => s.Subject.Name).ToList() ?? new List<string>(),
            IsActive = true,
            UpcomingCount = upcomingCount,
            CompletedCount = completedCount,
            AvgRating = null,
            UpcomingLessons = upcomingLessons,
            LessonHistory = history,
        };
    }

    public async Task<PaymentsOverviewResponse> GetPaymentsAsync(Guid parentUserId, CancellationToken ct = default)
    {
        var studentIds = await ActiveStudentIdsAsync(parentUserId, ct);
        var students = await _db.Users.Where(u => studentIds.Contains(u.Id)).ToListAsync(ct);

        var now = DateTime.UtcNow;
        var children = new List<ChildPaymentSummaryDto>();

        foreach (var student in students)
        {
            var thisMonthInvoices = await _db.Invoices
                .Where(i => i.ParentUserId == parentUserId
                    && i.StudentUserId == student.Id
                    && i.Year == now.Year
                    && i.Month == now.Month)
                .OrderBy(i => i.IssuedAt)
                .ToListAsync(ct);
            var pendingInvoice = thisMonthInvoices.FirstOrDefault(i => i.Status == InvoiceStatus.Pending);

            children.Add(new ChildPaymentSummaryDto
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                ThisMonthAmount = thisMonthInvoices.Sum(i => i.AmountTotal),
                Status = thisMonthInvoices.Count == 0 || pendingInvoice is not null
                    ? InvoiceStatus.Pending
                    : InvoiceStatus.Paid,
                PendingInvoiceId = pendingInvoice?.Id,
            });
        }

        var history = await (
            from i in _db.Invoices
            join s in _db.Users on i.StudentUserId equals s.Id
            where i.ParentUserId == parentUserId
            orderby i.Year descending, i.Month descending
            select new InvoiceDto
            {
                InvoiceId = i.Id,
                StudentName = s.FullName,
                PeriodDescription = MonthName(i.Month) + " " + i.Year + " — " + i.LessonCount + " lessons",
                Amount = i.AmountTotal,
                Status = i.Status,
            }
        ).ToListAsync(ct);

        return new PaymentsOverviewResponse { Children = children, History = history };
    }

    public async Task<bool> PayInvoiceAsync(Guid parentUserId, Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(
            i => i.Id == invoiceId && i.ParentUserId == parentUserId, ct);

        if (invoice is null || invoice.Status == InvoiceStatus.Paid)
        {
            return false;
        }

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task InviteStudentAsync(
        Guid parentUserId, string email, Relationship relationship, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existingStudent = await _db.Users.FirstOrDefaultAsync(
            u => u.Email == normalizedEmail && u.Role == UserRole.Student, ct);

        var link = new ParentStudentLink
        {
            ParentUserId = parentUserId,
            StudentUserId = existingStudent?.Id,
            InviteEmail = existingStudent is null ? normalizedEmail : null,
            Relationship = relationship,
            Status = LinkStatus.Pending,
        };

        _db.ParentStudentLinks.Add(link);

        var parentName = await _db.Users.Where(u => u.Id == parentUserId).Select(u => u.FullName).FirstOrDefaultAsync(ct) ?? "A parent";
        await _emails.StageAsync(new EmailRequest
        {
            Category = EmailCategory.StudentInvite,
            ToEmail = normalizedEmail,
            RecipientUserId = existingStudent?.Id,
            Content = _templates.StudentInvite(parentName, normalizedEmail),
            RelatedEntityType = "ParentStudentLink",
            RelatedEntityId = link.Id,
            IdempotencyKey = $"student-invite:{parentUserId}:{normalizedEmail}",
            CreatedByUserId = parentUserId,
        }, ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> LinkByCodeAsync(Guid parentUserId, string code, CancellationToken ct = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var profile = await _db.StudentProfiles.FirstOrDefaultAsync(p => p.LinkCode == normalizedCode, ct);
        if (profile is null)
        {
            return false;
        }

        var alreadyLinked = await _db.ParentStudentLinks.AnyAsync(
            l => l.ParentUserId == parentUserId && l.StudentUserId == profile.UserId && l.Status == LinkStatus.Active, ct);

        if (alreadyLinked)
        {
            return true;
        }

        _db.ParentStudentLinks.Add(new ParentStudentLink
        {
            ParentUserId = parentUserId,
            StudentUserId = profile.UserId,
            Relationship = Relationship.Guardian,
            Status = LinkStatus.Active,
            AcceptedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<List<Guid>> ActiveStudentIdsAsync(Guid parentUserId, CancellationToken ct)
    {
        return await _db.ParentStudentLinks
            .Where(l => l.ParentUserId == parentUserId && l.Status == LinkStatus.Active && l.StudentUserId != null)
            .Select(l => l.StudentUserId!.Value)
            .ToListAsync(ct);
    }

    private async Task<List<ChildSummaryDto>> BuildChildSummariesAsync(List<Guid> studentIds, CancellationToken ct)
    {
        var students = await _db.Users.Where(u => studentIds.Contains(u.Id)).ToListAsync(ct);
        var profiles = await _db.StudentProfiles
            .Include(p => p.Subjects).ThenInclude(s => s.Subject)
            .Where(p => studentIds.Contains(p.UserId))
            .ToListAsync(ct);

        var result = new List<ChildSummaryDto>();
        foreach (var student in students)
        {
            var profile = profiles.FirstOrDefault(p => p.UserId == student.Id);

            var upcoming = await _db.Bookings.CountAsync(
                b => b.StudentUserId == student.Id && b.Status == BookingStatus.Upcoming, ct);
            var completed = await _db.Bookings.CountAsync(
                b => b.StudentUserId == student.Id && b.Status == BookingStatus.Completed, ct);

            result.Add(new ChildSummaryDto
            {
                StudentId = student.Id,
                FullName = student.FullName,
                YearLevel = profile?.YearLevel,
                UpcomingCount = upcoming,
                CompletedCount = completed,
                Subjects = profile?.Subjects.Select(s => s.Subject.Name).ToList() ?? new List<string>(),
            });
        }

        return result;
    }

    private async Task<List<UpcomingLessonDto>> UpcomingLessonsAsync(
        IReadOnlyCollection<Guid> studentIds, int take, CancellationToken ct)
    {
        return await (
            from b in _db.Bookings
            join student in _db.Users on b.StudentUserId equals student.Id
            join tutor in _db.Users on b.TutorUserId equals tutor.Id
            where studentIds.Contains(b.StudentUserId) && b.Status == BookingStatus.Upcoming
            orderby b.ScheduledAt
            select new UpcomingLessonDto
            {
                BookingId = b.Id,
                SubjectName = b.Subject.Name,
                StudentName = student.FullName,
                TutorName = tutor.FullName,
                ScheduledAt = b.ScheduledAt,
                Price = b.PriceAmount,
            }
        ).Take(take).ToListAsync(ct);
    }

    private async Task<InvoiceDto?> OutstandingInvoiceAsync(Guid parentUserId, CancellationToken ct)
    {
        return await (
            from i in _db.Invoices
            join s in _db.Users on i.StudentUserId equals s.Id
            where i.ParentUserId == parentUserId && i.Status == InvoiceStatus.Pending
            orderby i.Year descending, i.Month descending
            select new InvoiceDto
            {
                InvoiceId = i.Id,
                StudentName = s.FullName,
                PeriodDescription = MonthName(i.Month) + " " + i.Year + " — " + i.LessonCount + " lessons",
                Amount = i.AmountTotal,
                Status = i.Status,
            }
        ).FirstOrDefaultAsync(ct);
    }

    private async Task<(decimal Amount, decimal ChangePercent)> MonthlyAmountWithChangeAsync(
        List<Guid> studentIds, DateTime now, CancellationToken ct)
    {
        var current = await _db.Invoices
            .Where(i => studentIds.Contains(i.StudentUserId) && i.Year == now.Year && i.Month == now.Month)
            .SumAsync(i => (decimal?)i.AmountTotal, ct) ?? 0m;

        var previousMonth = now.AddMonths(-1);
        var previous = await _db.Invoices
            .Where(i => studentIds.Contains(i.StudentUserId) && i.Year == previousMonth.Year && i.Month == previousMonth.Month)
            .SumAsync(i => (decimal?)i.AmountTotal, ct) ?? 0m;

        return (current, PercentChange(current, previous));
    }

    private async Task<(int Total, decimal ChangePercent)> TotalSessionsWithChangeAsync(
        List<Guid> studentIds, DateTime now, CancellationToken ct)
    {
        var total = await _db.Bookings.CountAsync(
            b => studentIds.Contains(b.StudentUserId) && b.Status == BookingStatus.Completed, ct);

        var currentMonthCount = await _db.Bookings.CountAsync(
            b => studentIds.Contains(b.StudentUserId) && b.Status == BookingStatus.Completed
                && b.ScheduledAt.Year == now.Year && b.ScheduledAt.Month == now.Month, ct);

        var previousMonth = now.AddMonths(-1);
        var previousMonthCount = await _db.Bookings.CountAsync(
            b => studentIds.Contains(b.StudentUserId) && b.Status == BookingStatus.Completed
                && b.ScheduledAt.Year == previousMonth.Year && b.ScheduledAt.Month == previousMonth.Month, ct);

        return (total, PercentChange(currentMonthCount, previousMonthCount));
    }

    private static decimal PercentChange(decimal current, decimal previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0m : 100m;
        }

        return Math.Round((current - previous) / previous * 100, 1);
    }

    private static string MonthName(int month) =>
        System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month);
}