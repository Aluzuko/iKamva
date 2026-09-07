using Backend.Infrastructure.Admin;
using Backend.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminMonitoringController : ControllerBase
{
    private readonly IMonitoringAdminService _monitoring;

    public AdminMonitoringController(IMonitoringAdminService monitoring)
    {
        _monitoring = monitoring;
    }

    [HttpGet("bookings")]
    public async Task<ActionResult<IReadOnlyList<AdminBookingDto>>> GetBookings(CancellationToken ct)
    {
        return Ok(await _monitoring.GetBookingsAsync(ct));
    }

    [HttpGet("payments")]
    public async Task<ActionResult<IReadOnlyList<AdminInvoiceDto>>> GetPayments(CancellationToken ct)
    {
        return Ok(await _monitoring.GetPaymentsAsync(ct));
    }
}
