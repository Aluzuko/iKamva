using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Infrastructure.Payments;
using Backend.Infrastructure.Students;
using Backend.Infrastructure.Tutors;
using Backend.Models.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/student/invoices")]
[Authorize(Roles = "student")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoices;
    private readonly IPayFastService _payFast;

    public InvoicesController(IInvoiceService invoices, IPayFastService payFast)
    {
        _invoices = invoices;
        _payFast = payFast;
    }

    [HttpPost]
    [ProducesResponseType(typeof(StudentInvoiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentInvoiceResponse>> Create(
        CreateInvoiceRequest request, CancellationToken ct)
    {
        var result = await _invoices.CreateForBookingAsync(CurrentUserId(), request.BookingId, ct);
        if (result.Outcome != ServiceOutcome.Ok || result.Value is null)
        {
            return result.Outcome switch
            {
                ServiceOutcome.NotFound => NotFound(new { message = result.Message }),
                ServiceOutcome.Conflict => Conflict(new { message = result.Message }),
                _ => BadRequest(new { message = result.Message }),
            };
        }

        var response = await AddCheckoutAsync(result.Value.Invoice, ct);
        if (!result.Value.Created)
        {
            return Ok(response);
        }

        return CreatedAtAction(nameof(Get), new { invoiceId = response.InvoiceId }, response);
    }

    [HttpGet("{invoiceId:guid}")]
    [ProducesResponseType(typeof(StudentInvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentInvoiceResponse>> Get(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await _invoices.GetForStudentAsync(CurrentUserId(), invoiceId, ct);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    private async Task<StudentInvoiceResponse> AddCheckoutAsync(StudentInvoiceResponse invoice, CancellationToken ct)
    {
        if (!invoice.PaymentRequired)
        {
            return invoice;
        }

        var checkout = await _payFast.CreateCheckoutAsync(CurrentUserId(), invoice.InvoiceId, ct);
        return checkout.Outcome == ServiceOutcome.Ok && checkout.Value is not null
            ? invoice with { PaymentRedirect = checkout.Value }
            : invoice with { PaymentError = checkout.Message ?? "Online payment could not be started." };
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(sub!);
    }
}