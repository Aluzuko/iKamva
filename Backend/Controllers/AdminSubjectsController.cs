using Backend.Infrastructure.Admin;
using Backend.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/subjects")]
[Authorize(Roles = "admin")]
public class AdminSubjectsController : ControllerBase
{
    private readonly ISubjectAdminService _subjectAdmin;

    public AdminSubjectsController(ISubjectAdminService subjectAdmin)
    {
        _subjectAdmin = subjectAdmin;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubjectPricingDto>>> GetSubjects(CancellationToken ct)
    {
        return Ok(await _subjectAdmin.GetSubjectsAsync(ct));
    }

    [HttpPost]
    public async Task<ActionResult> CreateSubject(CreateSubjectRequest request, CancellationToken ct)
    {
        var id = await _subjectAdmin.CreateSubjectAsync(request, ct);
        return Ok(new { subjectId = id });
    }

    [HttpPut("{subjectId:guid}/pricing")]
    public async Task<IActionResult> UpdatePricing(
        Guid subjectId, UpdateSubjectPricingRequest request, CancellationToken ct)
    {
        var success = await _subjectAdmin.UpdatePricingAsync(subjectId, request, ct);
        return success ? NoContent() : NotFound();
    }
}
