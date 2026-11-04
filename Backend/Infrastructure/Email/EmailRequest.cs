using Backend.Domain.Enums;

namespace Backend.Infrastructure.Email;

public sealed record EmailAttachmentInput(string FileName, string ContentType, byte[] Content, string? ContentId = null);

public sealed record RenderedEmail(string Subject, string Html, string Text);

public sealed class EmailRequest
{
    public required EmailCategory Category { get; init; }
    public required string ToEmail { get; init; }
    public string? ToName { get; init; }
    public IReadOnlyList<string> Cc { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Bcc { get; init; } = Array.Empty<string>();
    public string? ReplyTo { get; init; }
    public Guid? RecipientUserId { get; init; }
    public required RenderedEmail Content { get; init; }
    public EmailPriority Priority { get; init; } = EmailPriority.Normal;
    public DateTime? SendAfter { get; init; }
    public bool IsSensitive { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? IdempotencyKey { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public int MaxAttempts { get; init; } = 5;
    public IReadOnlyList<EmailAttachmentInput> Attachments { get; init; } = Array.Empty<EmailAttachmentInput>();
}
