using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enums;

namespace Backend.Models.Parent;

public sealed class InviteStudentRequest
{
    [Required, EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required Relationship Relationship { get; init; }
}