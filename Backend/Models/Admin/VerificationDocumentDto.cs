using Backend.Domain.Enums;

namespace Backend.Models.Admin;

public sealed class VerificationDocumentDto
{
    public required Guid DocumentId { get; init; }
    public required DocumentType DocumentType { get; init; }
    public required string FileName { get; init; }
    public required string FileUrl { get; init; }
    public required DateTime UploadedAt { get; init; }
}
