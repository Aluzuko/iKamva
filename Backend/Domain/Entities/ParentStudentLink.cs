using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public class ParentStudentLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParentUserId { get; set; }
    public Guid? StudentUserId { get; set; }
    public string? InviteEmail { get; set; }
    public Relationship Relationship { get; set; }
    public LinkStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
}