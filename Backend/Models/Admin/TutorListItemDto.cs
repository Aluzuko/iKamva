using Backend.Domain.Enums;

namespace Backend.Models.Admin;

public sealed class TutorListItemDto
{
    public required Guid TutorId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required decimal HourlyRate { get; init; }
    public required int YearsExperience { get; init; }
    public required VerificationStatus VerificationStatus { get; init; }
    public required bool IsActive { get; init; }
    public required IReadOnlyList<string> Subjects { get; init; }
    public required int CompletedBookings { get; init; }
}
