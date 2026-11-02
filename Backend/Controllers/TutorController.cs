using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Infrastructure.Tutors;
using Backend.Models.Tutor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/tutor")]
[Authorize(Roles = "tutor")]
public class TutorController : ControllerBase
{
    private readonly ITutorProfileService _profiles;
    private readonly ITutorAvailabilityService _availability;
    private readonly ITutorLessonService _lessons;
    private readonly ITutorEarningsService _earnings;

    public TutorController(
        ITutorProfileService profiles,
        ITutorAvailabilityService availability,
        ITutorLessonService lessons,
        ITutorEarningsService earnings)
    {
        _profiles = profiles;
        _availability = availability;
        _lessons = lessons;
        _earnings = earnings;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<TutorDashboardResponse>> Dashboard(CancellationToken ct) =>
        Ok(await _lessons.GetDashboardAsync(CurrentUserId(), ct));

    [HttpGet("profile")]
    public async Task<ActionResult<TutorProfileResponse>> GetProfile(CancellationToken ct)
    {
        var profile = await _profiles.GetProfileAsync(CurrentUserId(), ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateTutorProfileRequest request, CancellationToken ct) =>
        ToAction(await _profiles.UpdateProfileAsync(CurrentUserId(), request, ct));

    [HttpGet("subjects")]
    public async Task<ActionResult<IReadOnlyList<TutorSubjectOptionDto>>> Subjects(CancellationToken ct) =>
        Ok(await _profiles.GetSubjectOptionsAsync(ct));

    [HttpPost("documents")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(TutorProfileService.MaxDocumentBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = TutorProfileService.MaxDocumentBytes + 64 * 1024)]
    public async Task<ActionResult<TutorDocumentDto>> UploadDocument([FromForm] UploadDocumentForm form, CancellationToken ct)
    {
        var result = await _profiles.UploadDocumentAsync(CurrentUserId(), form.DocumentType!.Value, form.File!, ct);
        return result.Outcome == ServiceOutcome.Ok ? Ok(result.Value) : ToAction(new ServiceResult(result.Outcome, result.Message));
    }

    [HttpDelete("documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid documentId, CancellationToken ct) =>
        ToAction(await _profiles.DeleteDocumentAsync(CurrentUserId(), documentId, ct));

    [HttpGet("documents/{documentId:guid}/file")]
    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken ct)
    {
        var file = await _profiles.OpenDocumentAsync(documentId, CurrentUserId(), ct);
        if (file is null)
        {
            return NotFound();
        }
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(file.Value.File.Content, file.Value.File.ContentType, file.Value.FileName);
    }

    [HttpPost("verification/submit")]
    public async Task<IActionResult> SubmitForReview(CancellationToken ct) =>
        ToAction(await _profiles.SubmitForReviewAsync(CurrentUserId(), ct));

    [HttpGet("availability")]
    public async Task<ActionResult<AvailabilityResponse>> GetAvailability(CancellationToken ct) =>
        Ok(await _availability.GetAsync(CurrentUserId(), ct));

    [HttpPut("availability")]
    public async Task<IActionResult> UpdateAvailability(UpdateAvailabilityRequest request, CancellationToken ct) =>
        ToAction(await _availability.ReplaceAsync(CurrentUserId(), request, ct));

    [HttpGet("lessons")]
    public async Task<ActionResult<IReadOnlyList<TutorLessonDto>>> Lessons(CancellationToken ct) =>
        Ok(await _lessons.GetLessonsAsync(CurrentUserId(), ct));

    [HttpPost("lessons/{bookingId:guid}/cancel")]
    public async Task<IActionResult> CancelLesson(Guid bookingId, CancelLessonRequest request, CancellationToken ct) =>
        ToAction(await _lessons.CancelAsync(CurrentUserId(), bookingId, request.Reason, ct));

    [HttpPost("lessons/{bookingId:guid}/complete")]
    public async Task<IActionResult> CompleteLesson(Guid bookingId, CancellationToken ct) =>
        ToAction(await _lessons.CompleteAsync(CurrentUserId(), bookingId, ct));

    [HttpPut("lessons/{bookingId:guid}/notes")]
    public async Task<IActionResult> UpdateNotes(Guid bookingId, UpdateLessonNotesRequest request, CancellationToken ct) =>
        ToAction(await _lessons.UpdateNotesAsync(CurrentUserId(), bookingId, request, ct));

    [HttpGet("feedback")]
    public async Task<ActionResult<FeedbackResponse>> Feedback(CancellationToken ct) =>
        Ok(await _lessons.GetFeedbackAsync(CurrentUserId(), ct));

    [HttpGet("earnings")]
    public async Task<ActionResult<EarningsResponse>> Earnings(CancellationToken ct) =>
        Ok(await _earnings.GetEarningsAsync(CurrentUserId(), ct));

    [HttpPost("payouts")]
    public async Task<ActionResult<PayoutDto>> RequestPayout(CancellationToken ct)
    {
        var result = await _earnings.RequestPayoutAsync(CurrentUserId(), ct);
        return result.Outcome == ServiceOutcome.Ok ? Ok(result.Value) : ToAction(new ServiceResult(result.Outcome, result.Message));
    }

    private ActionResult ToAction(ServiceResult result) => result.Outcome switch
    {
        ServiceOutcome.Ok => NoContent(),
        ServiceOutcome.NotFound => NotFound(result.Message is null ? null : new { message = result.Message }),
        ServiceOutcome.Conflict => Conflict(new { message = result.Message }),
        _ => BadRequest(new { message = result.Message }),
    };

    private Guid CurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(sub!);
    }
}
