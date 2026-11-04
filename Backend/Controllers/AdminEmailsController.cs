using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Domain.Enums;
using Backend.Infrastructure.Email;
using Backend.Infrastructure.Tutors;
using Backend.Models.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/emails")]
[Authorize(Roles = "admin")]
public class AdminEmailsController : ControllerBase
{
    private readonly IEmailAdminService _emails;
    private readonly IEmailQueueProcessor _processor;

    public AdminEmailsController(IEmailAdminService emails, IEmailQueueProcessor processor)
    {
        _emails = emails;
        _processor = processor;
    }

    [HttpGet]
    public async Task<ActionResult<EmailPageResponse>> List(
        [FromQuery] EmailStatus? status, [FromQuery] EmailCategory? category, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _emails.ListAsync(status, category, search, page, pageSize, ct));

    [HttpGet("stats")]
    public async Task<ActionResult<EmailStatsResponse>> Stats(CancellationToken ct) => Ok(await _emails.StatsAsync(ct));

    [HttpGet("{emailId:guid}")]
    public async Task<ActionResult<EmailDetailDto>> Get(Guid emailId, CancellationToken ct)
    {
        var email = await _emails.GetAsync(emailId, ct);
        return email is null ? NotFound() : Ok(email);
    }

    [HttpGet("{emailId:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> Attachment(Guid emailId, Guid attachmentId, CancellationToken ct)
    {
        var file = await _emails.OpenAttachmentAsync(emailId, attachmentId, ct);
        if (file is null)
        {
            return NotFound();
        }
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(file.Value.File.Content, file.Value.File.ContentType, file.Value.FileName);
    }

    [HttpPost("process")]
    public async Task<ActionResult<EmailProcessResult>> Process(ProcessQueueRequest request, CancellationToken ct) =>
        Ok(await _processor.ProcessAsync(request.BatchSize, ct));

    [HttpPost("{emailId:guid}/retry")]
    public async Task<IActionResult> Retry(Guid emailId, CancellationToken ct) => ToAction(await _emails.RetryAsync(emailId, ct));

    [HttpPost("{emailId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid emailId, CancellationToken ct) => ToAction(await _emails.CancelAsync(emailId, ct));

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
    public async Task<ActionResult> Compose([FromForm] ComposeLetterForm form, CancellationToken ct)
    {
        var result = await _emails.ComposeLetterAsync(CurrentUserId(), form, ct);
        return result.Outcome == ServiceOutcome.Ok ? Ok(new { emailId = result.Value }) : ToAction(new ServiceResult(result.Outcome, result.Message));
    }

    [HttpPost("invoices/{invoiceId:guid}")]
    public async Task<ActionResult> EmailInvoice(Guid invoiceId, CancellationToken ct)
    {
        var result = await _emails.QueueInvoiceAsync(CurrentUserId(), invoiceId, ct);
        return result.Outcome == ServiceOutcome.Ok ? Ok(new { emailId = result.Value }) : ToAction(new ServiceResult(result.Outcome, result.Message));
    }

    [HttpPost("purge")]
    public async Task<ActionResult> Purge([FromQuery] int olderThanDays = 90, CancellationToken ct = default)
    {
        var removed = await _processor.PurgeAsync(TimeSpan.FromDays(Math.Clamp(olderThanDays, 7, 3650)), ct);
        return Ok(new { removed });
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
