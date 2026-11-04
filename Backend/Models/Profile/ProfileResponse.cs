using Backend.Domain.Enums;

namespace Backend.Models.Profile;

public sealed class ProfileResponse
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required bool IsEmailVerified { get; init; }
    public required string FullName { get; init; }
    public required UserRole Role { get; init; }
    public required DateTime CreatedAt { get; init; }

    public string? YearLevel { get; init; }
    public string? LinkCode { get; init; }
    public IReadOnlyList<string>? EnrolledSubjects { get; init; }

    public string? Bio { get; init; }
    public decimal? HourlyRate { get; init; }
    public int? YearsExperience { get; init; }
    public VerificationStatus? VerificationStatus { get; init; }
    public IReadOnlyList<string>? TutorSubjects { get; init; }
}
