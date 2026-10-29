using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Auth;

public sealed class ChangeEmailRequest
{
    [Required]
    public required string CurrentPassword { get; init; }

    [Required, EmailAddress]
    public required string NewEmail { get; init; }
}
