using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Infrastructure.Payments;
using Backend.Infrastructure.Tutors;
using Backend.Models.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers;

[ApiController]
public class PaymentsController : ControllerBase
{
    private const int MaxNotificationBytes = 64 * 1024;

    private readonly IPayFastService _payFast;

    public PaymentsController(IPayFastService payFast)
    {
        _payFast = payFast;
    }

    [HttpPost("api/parent/payments/{invoiceId:guid}/checkout")]
    [Authorize(Roles = "parent")]
    [EnableRateLimiting("UserActions")]
    public async Task<ActionResult<PaymentRedirectResponse>> Checkout(Guid invoiceId, CancellationToken ct)
    {
        var result = await _payFast.CreateCheckoutAsync(CurrentUserId(), invoiceId, ct);
        return result.Outcome switch
        {
            ServiceOutcome.Ok => Ok(result.Value),
            ServiceOutcome.NotFound => NotFound(),
            ServiceOutcome.Conflict => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message }),
        };
    }

    [HttpGet("api/parent/payments/attempts/{attemptId:guid}")]
    [Authorize(Roles = "parent")]
    public async Task<ActionResult<PaymentAttemptDto>> Attempt(Guid attemptId, CancellationToken ct)
    {
        var attempt = await _payFast.GetAttemptAsync(CurrentUserId(), attemptId, ct);
        return attempt is null ? NotFound() : Ok(attempt);
    }

    [HttpGet("api/payments/payfast/return")]
    [AllowAnonymous]
    public IActionResult Return([FromQuery] Guid? attempt) =>
        Redirect(_payFast.ClientResultUrl(attempt, cancelled: false));

    [HttpGet("api/payments/payfast/cancel")]
    [AllowAnonymous]
    public async Task<IActionResult> Cancel([FromQuery] Guid? attempt, CancellationToken ct)
    {
        if (attempt is not null)
        {
            await _payFast.MarkCancelledAsync(attempt.Value, ct);
        }
        return Redirect(_payFast.ClientResultUrl(attempt, cancelled: true));
    }

    [HttpPost("api/payments/payfast/notify")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    [RequestSizeLimit(MaxNotificationBytes)]
    public async Task<IActionResult> Notify(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        await _payFast.HandleNotificationAsync(body, HttpContext.Connection.RemoteIpAddress, ct);
        return Ok();
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(sub!);
    }
}
