namespace Backend.Models.Parent;

public sealed class PaymentsOverviewResponse
{
    public required IReadOnlyList<ChildPaymentSummaryDto> Children { get; init; }
    public required IReadOnlyList<InvoiceDto> History { get; init; }
}