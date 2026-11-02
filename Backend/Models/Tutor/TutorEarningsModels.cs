using Backend.Domain.Enums;
using Backend.Models.Admin;

namespace Backend.Models.Tutor;

public sealed class EarningLineDto
{
    public required Guid BookingId { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public required string StudentName { get; init; }
    public required string SubjectName { get; init; }
    public required decimal Price { get; init; }
    public required decimal CommissionPercent { get; init; }
    public required decimal Earning { get; init; }
}

public sealed class PayoutDto
{
    public required Guid PayoutId { get; init; }
    public required decimal Amount { get; init; }
    public required PayoutStatus Status { get; init; }
    public string? Reference { get; init; }
    public required DateTime RequestedAt { get; init; }
    public DateTime? PaidAt { get; init; }
}

public sealed class EarningsResponse
{
    public required decimal TotalEarned { get; init; }
    public required decimal AvailableBalance { get; init; }
    public required decimal PendingPayouts { get; init; }
    public required decimal PaidOut { get; init; }
    public required decimal ThisMonth { get; init; }
    public required decimal MinimumPayout { get; init; }
    public required bool CanRequestPayout { get; init; }
    public string? PayoutBlockedReason { get; init; }
    public required IReadOnlyList<MonthlyPointDto> Monthly { get; init; }
    public required IReadOnlyList<EarningLineDto> RecentLessons { get; init; }
    public required IReadOnlyList<PayoutDto> Payouts { get; init; }
}
