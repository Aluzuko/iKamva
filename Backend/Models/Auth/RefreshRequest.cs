using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Auth;

public sealed class RefreshRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}
