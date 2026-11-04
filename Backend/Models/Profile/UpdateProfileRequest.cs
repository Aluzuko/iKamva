using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Profile;

public sealed class UpdateProfileRequest
{
    [Required, MinLength(2)]
    public required string FullName { get; init; }

    public string? Bio { get; init; }

    public decimal? HourlyRate { get; init; }

    public string? YearLevel { get; init; }
}
