using Backend.Domain.Enums;

namespace Backend.Models.Parent;

public sealed class ChildPaymentSummaryDto
{
    public required Guid StudentId { get; init; }
    public required string StudentName { get; init; }
    public required decimal ThisMonthAmount { get; init; }
    public required InvoiceStatus Status { get; init; }
    public Guid? PendingInvoiceId { get; init; }
}