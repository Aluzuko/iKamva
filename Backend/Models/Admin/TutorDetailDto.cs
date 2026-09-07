using Backend.Domain.Enums;

namespace Backend.Models.Admin;

public sealed class TutorDetailDto
{
    public required Guid TutorId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? Bio { get; init; }
    public required decimal HourlyRate { get; init; }
    public required int YearsExperience { get; init; }
    public required VerificationStatus VerificationStatus { get; init; }
    public string? RejectionReason { get; init; }
    public required IReadOnlyList<string> Subjects { get; init; }
    public required IReadOnlyList<VerificationDocumentDto> Documents { get; init; }
}
