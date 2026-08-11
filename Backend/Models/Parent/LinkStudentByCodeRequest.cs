using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Parent;

public sealed class LinkStudentByCodeRequest
{
    [Required]
    public required string Code { get; init; }
}