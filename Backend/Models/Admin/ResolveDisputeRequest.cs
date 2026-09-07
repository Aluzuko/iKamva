using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Admin;

public sealed class ResolveDisputeRequest
{
    [Required]
    public required string Resolution { get; init; }
}
