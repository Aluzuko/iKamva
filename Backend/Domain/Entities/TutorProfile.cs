using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public class TutorProfile
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Bio { get; set; }
    public decimal HourlyRate { get; set; }
    public int YearsExperience { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public string? RejectionReason { get; set; }
    public ICollection<TutorSubject> Subjects { get; set; } = new List<TutorSubject>();
    public ICollection<VerificationDocument> Documents { get; set; } = new List<VerificationDocument>();
}
