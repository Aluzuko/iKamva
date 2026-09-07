using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public class VerificationDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TutorUserId { get; set; }
    public TutorProfile TutorProfile { get; set; } = null!;
    public DocumentType DocumentType { get; set; }
    public required string FileName { get; set; }
    public required string FileUrl { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
