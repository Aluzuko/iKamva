using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enums;

namespace Backend.Models.Email;

public sealed class EmailListItemDto
{
    public required Guid EmailId { get; init; }
    public required EmailCategory Category { get; init; }
    public required EmailStatus Status { get; init; }
    public required EmailPriority Priority { get; init; }
    public required string ToEmail { get; init; }
    public string? ToName { get; init; }
    public required string Subject { get; init; }
    public required int Attempts { get; init; }
    public required int MaxAttempts { get; init; }
    public required int AttachmentCount { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime NextAttemptAt { get; init; }
    public DateTime? SentAt { get; init; }
    public string? LastError { get; init; }
}

public sealed class EmailAttachmentDto
{
    public required Guid AttachmentId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required bool IsInline { get; init; }
}

public sealed class EmailDetailDto
{
    public required EmailListItemDto Summary { get; init; }
    public string? CcEmails { get; init; }
    public string? BccEmails { get; init; }
    public string? ReplyToEmail { get; init; }
    public required string HtmlBody { get; init; }
    public string? TextBody { get; init; }
    public required bool IsSensitive { get; init; }
    public required bool BodyPurged { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public DateTime? LastAttemptAt { get; init; }
    public string? ProviderMessageId { get; init; }
    public required IReadOnlyList<EmailAttachmentDto> Attachments { get; init; }
}

public sealed class EmailPageResponse
{
    public required IReadOnlyList<EmailListItemDto> Items { get; init; }
    public required int Total { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

public sealed class EmailStatsResponse
{
    public required int Pending { get; init; }
    public required int Due { get; init; }
    public required int Sending { get; init; }
    public required int Sent { get; init; }
    public required int Failed { get; init; }
    public required int Cancelled { get; init; }
    public DateTime? OldestDueAt { get; init; }
    public required bool WorkerEnabled { get; init; }
    public required string Transport { get; init; }
}

public sealed class ProcessQueueRequest
{
    [Range(1, 200)]
    public int BatchSize { get; init; } = 50;
}

public sealed class ComposeLetterForm
{
    [Required, EmailAddress, MaxLength(320)]
    public string? ToEmail { get; init; }

    [MaxLength(200)]
    public string? ToName { get; init; }

    [Required, MinLength(2), MaxLength(300)]
    public string? Subject { get; init; }

    [Required, MinLength(2), MaxLength(20000)]
    public string? Message { get; init; }

    public List<IFormFile> Files { get; init; } = new();
}
