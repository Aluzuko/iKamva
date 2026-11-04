using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public class EmailMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EmailCategory Category { get; set; }
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;

    public required string ToEmail { get; set; }
    public string? ToName { get; set; }
    public string? CcEmails { get; set; }
    public string? BccEmails { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string? ReplyToEmail { get; set; }
    public Guid? RecipientUserId { get; set; }

    public required string Subject { get; set; }
    public required string HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public bool IsSensitive { get; set; }
    public bool BodyPurged { get; set; }

    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? IdempotencyKey { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public DateTime? LockedUntil { get; set; }
    public string? LockedBy { get; set; }

    public string? ProviderMessageId { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
}
