namespace Backend.Domain.Entities;

public class TutorSubject
{
    public Guid TutorUserId { get; set; }
    public TutorProfile TutorProfile { get; set; } = null!;
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
}
