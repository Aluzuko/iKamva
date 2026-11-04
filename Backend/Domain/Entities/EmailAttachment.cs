namespace Backend.Domain.Entities;

public class EmailAttachment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmailMessageId { get; set; }
    public EmailMessage EmailMessage { get; set; } = null!;
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public required string StorageKey { get; set; }
    public string? ContentId { get; set; }
    public bool IsInline { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
