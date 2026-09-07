using Backend.Domain.Enums;

namespace Backend.Models.Admin;

public sealed class AdminInvoiceDto
{
    public required Guid InvoiceId { get; init; }
    public required string ParentName { get; init; }
    public required string StudentName { get; init; }
    public required string PeriodDescription { get; init; }
    public required decimal Amount { get; init; }
    public required InvoiceStatus Status { get; init; }
}
