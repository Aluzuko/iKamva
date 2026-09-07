using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Admin;

public sealed class RejectTutorRequest
{
    [Required]
    public required string Reason { get; init; }
}
