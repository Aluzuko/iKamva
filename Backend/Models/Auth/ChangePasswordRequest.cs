using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Auth;

public sealed class ChangePasswordRequest
{
    [Required]
    public required string CurrentPassword { get; init; }

    [Required, MinLength(6)]
    public required string NewPassword { get; init; }
}
