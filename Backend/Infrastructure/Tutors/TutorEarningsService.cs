using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Models.Admin;
using Backend.Models.Tutor;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Tutors;

public interface ITutorEarningsService
{
    Task<EarningsResponse> GetEarningsAsync(Guid tutorId, CancellationToken ct = default);

    Task<ServiceResult<PayoutDto>> RequestPayoutAsync(Guid tutorId, CancellationToken ct = default);
}

public class TutorEarningsService : ITutorEarningsService
{
    private readonly AppDbContext _db;

    public TutorEarningsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<EarningsResponse> GetEarningsAsync(Guid tutorId, CancellationToken ct = default)
    {
        var lines = await CompletedLinesAsync(tutorId, ct);
        var payouts = await _db.Payouts
            .Where(p => p.TutorUserId == tutorId)
            .OrderByDescending(p => p.RequestedAt)
            .ToListAsync(ct);
        var profile = await _db.TutorProfiles.FirstOrDefaultAsync(t => t.UserId == tutorId, ct);

        var totalEarned = lines.Sum(l => l.Earning);
        var pending = payouts.Where(p => p.Status == PayoutStatus.Requested).Sum(p => p.Amount);
        var paid = payouts.Where(p => p.Status == PayoutStatus.Paid).Sum(p => p.Amount);
        var available = Math.Max(0m, totalEarned - pending - paid);
        var blocked = PayoutBlockedReason(profile, available, payouts);

        var localNow = TutorRules.ToLocal(DateTime.UtcNow);
        var monthly = new List<MonthlyPointDto>();
        for (var i = 5; i >= 0; i--)
        {
            var month = new DateTime(localNow.Year, localNow.Month, 1).AddMonths(-i);
            var value = lines
                .Where(l => { var d = TutorRules.ToLocal(l.ScheduledAt); return d.Year == month.Year && d.Month == month.Month; })
                .Sum(l => l.Earning);
            monthly.Add(new MonthlyPointDto { Label = month.ToString("MMM"), Value = value });
        }

        return new EarningsResponse
        {
            TotalEarned = totalEarned,
            AvailableBalance = available,
            PendingPayouts = pending,
            PaidOut = paid,
            ThisMonth = monthly[^1].Value,
            MinimumPayout = TutorRules.MinimumPayout,
            CanRequestPayout = blocked is null,
            PayoutBlockedReason = blocked,
            Monthly = monthly,
            RecentLessons = lines.Take(50).ToList(),
            Payouts = payouts.Select(Map).ToList(),
        };
    }

    public async Task<ServiceResult<PayoutDto>> RequestPayoutAsync(Guid tutorId, CancellationToken ct = default)
    {
        var earnings = await GetEarningsAsync(tutorId, ct);
        if (!earnings.CanRequestPayout)
        {
            return ServiceResult<PayoutDto>.From(ServiceResult.Conflict(earnings.PayoutBlockedReason ?? "You can't request a payout right now."));
        }

        var payout = new Payout { TutorUserId = tutorId, Amount = earnings.AvailableBalance };
        _db.Payouts.Add(payout);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<PayoutDto>.Ok(Map(payout));
    }

    private async Task<List<EarningLineDto>> CompletedLinesAsync(Guid tutorId, CancellationToken ct)
    {
        var rows = await (
            from b in _db.Bookings
            join s in _db.Users on b.StudentUserId equals s.Id
            where b.TutorUserId == tutorId && b.Status == BookingStatus.Completed
            orderby b.ScheduledAt descending
            select new { b.Id, b.ScheduledAt, StudentName = s.FullName, SubjectName = b.Subject.Name, b.PriceAmount, b.CommissionPercent }
        ).ToListAsync(ct);

        return rows.Select(r => new EarningLineDto
        {
            BookingId = r.Id,
            ScheduledAt = r.ScheduledAt,
            StudentName = r.StudentName,
            SubjectName = r.SubjectName,
            Price = r.PriceAmount,
            CommissionPercent = r.CommissionPercent,
            Earning = TutorRules.Earning(r.PriceAmount, r.CommissionPercent),
        }).ToList();
    }

    private static string? PayoutBlockedReason(TutorProfile? profile, decimal available, IReadOnlyList<Payout> payouts)
    {
        if (profile?.VerificationStatus != VerificationStatus.Approved)
        {
            return "Payouts open once your profile is verified.";
        }
        if (payouts.Any(p => p.Status == PayoutStatus.Requested))
        {
            return "You already have a payout being processed.";
        }
        if (available < TutorRules.MinimumPayout)
        {
            return $"You can request a payout once your balance reaches R{TutorRules.MinimumPayout:0}.";
        }
        return null;
    }

    private static PayoutDto Map(Payout p) => new()
    {
        PayoutId = p.Id,
        Amount = p.Amount,
        Status = p.Status,
        Reference = p.Reference,
        RequestedAt = p.RequestedAt,
        PaidAt = p.PaidAt,
    };
}
