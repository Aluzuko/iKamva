using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Admin;

public sealed class CreateTutorRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required, MinLength(2)]
    public required string FullName { get; init; }

    [Required, MinLength(6)]
    public required string Password { get; init; }

    [Required]
    public required decimal HourlyRate { get; init; }

    public int YearsExperience { get; init; }

    public string? Bio { get; init; }

    public IReadOnlyList<Guid> SubjectIds { get; init; } = Array.Empty<Guid>();
}
