using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Auth;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
