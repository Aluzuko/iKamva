using Backend.Domain.Enums;

namespace Backend.Models.Parent;

public sealed class InvoiceDto
{
    public required Guid InvoiceId { get; init; }
    public required string StudentName { get; init; }
    public required string PeriodDescription { get; init; }
    public required decimal Amount { get; init; }
    public required InvoiceStatus Status { get; init; }
}