namespace Backend.Domain.Entities;

public class StudentProfile
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string? YearLevel { get; set; }
    public required string LinkCode { get; set; }
    public ICollection<StudentSubject> Subjects { get; set; } = new List<StudentSubject>();
}