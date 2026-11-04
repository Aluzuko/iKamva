using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enums;

namespace Backend.Models.Messages;

public sealed class ConversationDto
{
    public required Guid ConversationId { get; init; }
    public required Guid OtherUserId { get; init; }
    public required string OtherName { get; init; }
    public required UserRole OtherRole { get; init; }
    public string? LastMessage { get; init; }
    public DateTime? LastMessageAt { get; init; }
    public required bool LastMessageMine { get; init; }
    public required int UnreadCount { get; init; }
}

public sealed class MessageDto
{
    public required Guid MessageId { get; init; }
    public required string Body { get; init; }
    public required DateTime SentAt { get; init; }
    public required bool Mine { get; init; }
    public DateTime? ReadAt { get; init; }
}

public sealed class MessagePageResponse
{
    public required ConversationDto Conversation { get; init; }
    public required IReadOnlyList<MessageDto> Messages { get; init; }
    public required bool HasMore { get; init; }
}

public sealed class ContactDto
{
    public required Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required UserRole Role { get; init; }
    public required string Context { get; init; }
}

public sealed class SendMessageRequest
{
    [Required, MinLength(1), MaxLength(2000)]
    public required string Body { get; init; }
}

public sealed class StartConversationRequest
{
    [Required]
    public required Guid RecipientUserId { get; init; }

    [Required, MinLength(1), MaxLength(2000)]
    public required string Body { get; init; }
}

public sealed class UnreadCountResponse
{
    public required int Unread { get; init; }
}
