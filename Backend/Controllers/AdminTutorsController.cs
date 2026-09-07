using Backend.Infrastructure.Admin;
using Backend.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/tutors")]
[Authorize(Roles = "admin")]
public class AdminTutorsController : ControllerBase
{
    private readonly ITutorAdminService _tutorAdmin;

    public AdminTutorsController(ITutorAdminService tutorAdmin)
    {
        _tutorAdmin = tutorAdmin;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TutorListItemDto>>> GetTutors(CancellationToken ct)
    {
        return Ok(await _tutorAdmin.GetTutorsAsync(ct));
    }

    [HttpGet("verification-queue")]
    public async Task<ActionResult<IReadOnlyList<TutorDetailDto>>> GetVerificationQueue(CancellationToken ct)
    {
        return Ok(await _tutorAdmin.GetVerificationQueueAsync(ct));
    }

    [HttpGet("{tutorId:guid}")]
    public async Task<ActionResult<TutorDetailDto>> GetTutor(Guid tutorId, CancellationToken ct)
    {
        var tutor = await _tutorAdmin.GetTutorAsync(tutorId, ct);
        return tutor is null ? NotFound() : Ok(tutor);
    }

    [HttpPost]
    public async Task<ActionResult> CreateTutor(CreateTutorRequest request, CancellationToken ct)
    {
        try
        {
            var id = await _tutorAdmin.CreateTutorAsync(request, ct);
            return CreatedAtAction(nameof(GetTutor), new { tutorId = id }, new { tutorId = id });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{tutorId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid tutorId, CancellationToken ct)
    {
        var success = await _tutorAdmin.ApproveTutorAsync(tutorId, ct);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("{tutorId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid tutorId, RejectTutorRequest request, CancellationToken ct)
    {
        var success = await _tutorAdmin.RejectTutorAsync(tutorId, request.Reason, ct);
        return success ? NoContent() : NotFound();
    }
}
