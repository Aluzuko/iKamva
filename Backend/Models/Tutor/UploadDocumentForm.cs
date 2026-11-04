using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enums;

namespace Backend.Models.Tutor;

public sealed class UploadDocumentForm
{
    [Required]
    public DocumentType? DocumentType { get; init; }

    [Required]
    public IFormFile? File { get; init; }
}
