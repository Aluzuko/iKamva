namespace Backend.Models.Admin;

public sealed class SubjectPricingDto
{
    public required Guid SubjectId { get; init; }
    public required string Name { get; init; }
    public required decimal DefaultHourlyRate { get; init; }
    public required decimal CommissionPercent { get; init; }
    public required int TutorCount { get; init; }
}
