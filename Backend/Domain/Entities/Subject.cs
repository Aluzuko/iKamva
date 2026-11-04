namespace Backend.Domain.Entities;

public class Subject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public decimal DefaultHourlyRate { get; set; }
    public decimal CommissionPercent { get; set; }
}