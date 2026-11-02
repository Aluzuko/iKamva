using Backend.Domain.Entities;
using Backend.Domain.Enums;

namespace Backend.Infrastructure.Tutors;

public static class TutorRules
{
    public const decimal MinimumPayout = 20m;

    public static readonly TimeZoneInfo Zone = ResolveZone();

    public static string VerificationState(TutorProfile? profile) => profile switch
    {
        null => "notSubmitted",
        { VerificationStatus: VerificationStatus.Approved } => "approved",
        { VerificationStatus: VerificationStatus.Rejected } => "rejected",
        { SubmittedAt: not null } => "inReview",
        _ => "notSubmitted",
    };

    public static bool CanEditDocuments(TutorProfile? profile) =>
        VerificationState(profile) is "notSubmitted" or "rejected";

    public static decimal Earning(decimal price, decimal commissionPercent) =>
        Math.Round(price * (100m - commissionPercent) / 100m, 2, MidpointRounding.AwayFromZero);

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateTime LocalToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);

    public static decimal ChangePercent(decimal current, decimal previous)
    {
        if (previous == 0m)
        {
            return current > 0m ? 100m : 0m;
        }
        return Math.Round((current - previous) / previous * 100m, 1);
    }

    private static TimeZoneInfo ResolveZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Johannesburg");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "South Africa Standard Time", "SAST");
        }
    }
}
