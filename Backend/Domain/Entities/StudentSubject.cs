namespace Backend.Domain.Entities;

public class StudentSubject
{
    public Guid StudentUserId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
}