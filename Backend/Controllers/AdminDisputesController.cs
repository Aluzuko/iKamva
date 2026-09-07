using Backend.Infrastructure.Admin;
using Backend.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/disputes")]
[Authorize(Roles = "admin")]
public class AdminDisputesController : ControllerBase
{
    private readonly IDisputeAdminService _disputeAdmin;

    public AdminDisputesController(IDisputeAdminService disputeAdmin)
    {
        _disputeAdmin = disputeAdmin;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DisputeDto>>> GetDisputes(CancellationToken ct)
    {
        return Ok(await _disputeAdmin.GetDisputesAsync(ct));
    }

    [HttpPost("{disputeId:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid disputeId, ResolveDisputeRequest request, CancellationToken ct)
    {
        var success = await _disputeAdmin.ResolveAsync(disputeId, request.Resolution, ct);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("{disputeId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid disputeId, ResolveDisputeRequest request, CancellationToken ct)
    {
        var success = await _disputeAdmin.RejectAsync(disputeId, request.Resolution, ct);
        return success ? NoContent() : NotFound();
    }
}
