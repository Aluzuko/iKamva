using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enums;
using Backend.Models.Payments;

namespace Backend.Models.Student;

/// <summary>Request to create or retrieve the invoice for one of the student's bookings.</summary>
public sealed record CreateInvoiceRequest
{
    [Required]
    public required Guid BookingId { get; init; }
}

/// <summary>Invoice details associated with a student's booking.</summary>
public sealed record StudentInvoiceResponse(
    Guid InvoiceId,
    Guid BookingId,
    string PeriodDescription,
    decimal Amount,
    InvoiceStatus Status,
    DateTime IssuedAt,
    bool PaymentRequired,
    PaymentRedirectResponse? PaymentRedirect = null,
    string? PaymentError = null);
