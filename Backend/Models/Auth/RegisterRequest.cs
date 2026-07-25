using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enums;

namespace Backend.Models.Auth;

public sealed class RegisterRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required, MinLength(6)]
    public required string Password { get; init; }

    [Required, MinLength(2), MaxLength(200)]
    public required string FullName { get; init; }

    [Required]
    public required UserRole Role { get; init; }
}
