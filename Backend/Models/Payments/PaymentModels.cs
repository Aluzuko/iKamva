using Backend.Domain.Enums;

namespace Backend.Models.Payments;

public sealed class PaymentField
{
    public required string Name { get; init; }
    public required string Value { get; init; }
}

public sealed class PaymentRedirectResponse
{
    public required Guid AttemptId { get; init; }
    public required string ProcessUrl { get; init; }
    public required IReadOnlyList<PaymentField> Fields { get; init; }
}

public sealed class PaymentAttemptDto
{
    public required Guid AttemptId { get; init; }
    public required Guid InvoiceId { get; init; }
    public required PaymentStatus Status { get; init; }
    public required InvoiceStatus InvoiceStatus { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
