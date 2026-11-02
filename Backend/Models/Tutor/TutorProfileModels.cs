using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enums;

namespace Backend.Models.Tutor;

public sealed class TutorSubjectOptionDto
{
    public required Guid SubjectId { get; init; }
    public required string Name { get; init; }
    public required decimal DefaultHourlyRate { get; init; }
}

public sealed class TutorDocumentDto
{
    public required Guid DocumentId { get; init; }
    public required DocumentType DocumentType { get; init; }
    public required string FileName { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime UploadedAt { get; init; }
}

public sealed class TutorProfileResponse
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? Bio { get; init; }
    public decimal? HourlyRate { get; init; }
    public int YearsExperience { get; init; }
    public required IReadOnlyList<Guid> SubjectIds { get; init; }
    public required string VerificationState { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public required bool CanEditDocuments { get; init; }
    public required IReadOnlyList<TutorDocumentDto> Documents { get; init; }
    public required IReadOnlyList<string> MissingForSubmission { get; init; }
}

public sealed class UpdateTutorProfileRequest
{
    [MaxLength(2000)]
    public string? Bio { get; init; }

    [Range(1, 10000)]
    public required decimal HourlyRate { get; init; }

    [Range(0, 80)]
    public int YearsExperience { get; init; }

    [MinLength(1)]
    public IReadOnlyList<Guid> SubjectIds { get; init; } = Array.Empty<Guid>();
}
