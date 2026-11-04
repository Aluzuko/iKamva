namespace Backend.Domain.Entities;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TutorUserId { get; set; }
    public Guid OtherUserId { get; set; }
    public string? LastMessagePreview { get; set; }
    public Guid? LastSenderUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
