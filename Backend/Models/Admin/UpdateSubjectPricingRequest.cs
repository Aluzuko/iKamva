using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Admin;

public sealed class UpdateSubjectPricingRequest
{
    [Required, Range(0, 10000)]
    public required decimal DefaultHourlyRate { get; init; }

    [Required, Range(0, 100)]
    public required decimal CommissionPercent { get; init; }
}
