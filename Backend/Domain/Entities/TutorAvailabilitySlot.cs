namespace Backend.Domain.Entities;

public class TutorAvailabilitySlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TutorUserId { get; set; }
    public TutorProfile TutorProfile { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public int StartMinute { get; set; }
    public int EndMinute { get; set; }
}
