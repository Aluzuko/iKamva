using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enums;

namespace Backend.Models.Auth;

public sealed class RegisterRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required, MinLength(8)]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one digit.")]
    public required string Password { get; init; }

    [Required, MinLength(2), MaxLength(200)]
    public required string FullName { get; init; }

    [Required]
    public required UserRole Role { get; init; }
}
