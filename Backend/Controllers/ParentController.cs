using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Infrastructure.Parents;
using Backend.Models.Parent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/parent")]
[Authorize(Roles = "parent")]
public class ParentController : ControllerBase
{
    private readonly IParentService _parentService;

    public ParentController(IParentService parentService)
    {
        _parentService = parentService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<FamilyDashboardResponse>> Dashboard(CancellationToken ct)
    {
        return Ok(await _parentService.GetDashboardAsync(CurrentUserId(), ct));
    }

    [HttpGet("children")]
    public async Task<ActionResult<IReadOnlyList<ChildSummaryDto>>> Children(CancellationToken ct)
    {
        return Ok(await _parentService.GetChildrenAsync(CurrentUserId(), ct));
    }

    [HttpGet("children/{studentId:guid}")]
    public async Task<ActionResult<StudentDetailResponse>> ChildDetail(Guid studentId, CancellationToken ct)
    {
        var detail = await _parentService.GetChildDetailAsync(CurrentUserId(), studentId, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("payments")]
    public async Task<ActionResult<PaymentsOverviewResponse>> Payments(CancellationToken ct)
    {
        return Ok(await _parentService.GetPaymentsAsync(CurrentUserId(), ct));
    }

    [HttpPost("payments/{invoiceId:guid}/pay")]
    public async Task<IActionResult> PayInvoice(Guid invoiceId, CancellationToken ct)
    {
        var success = await _parentService.PayInvoiceAsync(CurrentUserId(), invoiceId, ct);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("link-student/invite")]
    public async Task<IActionResult> InviteStudent(InviteStudentRequest request, CancellationToken ct)
    {
        await _parentService.InviteStudentAsync(CurrentUserId(), request.Email, request.Relationship, ct);
        return NoContent();
    }

    [HttpPost("link-student/code")]
    public async Task<IActionResult> LinkByCode(LinkStudentByCodeRequest request, CancellationToken ct)
    {
        var success = await _parentService.LinkByCodeAsync(CurrentUserId(), request.Code, ct);
        return success ? NoContent() : NotFound(new { message = "No student found with that code." });
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(sub!);
    }
}