using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Infrastructure.Messaging;
using Backend.Infrastructure.Tutors;
using Backend.Models.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize(Roles = "student,parent,tutor")]
public class MessagesController : ControllerBase
{
    private readonly IMessagingService _messaging;

    public MessagesController(IMessagingService messaging)
    {
        _messaging = messaging;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> Conversations(CancellationToken ct) =>
        Ok(await _messaging.GetConversationsAsync(CurrentUserId(), ct));

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<ActionResult<MessagePageResponse>> Messages(Guid conversationId, [FromQuery] DateTime? before, CancellationToken ct)
    {
        var page = await _messaging.GetMessagesAsync(CurrentUserId(), conversationId, before, ct);
        return page is null ? NotFound() : Ok(page);
    }

    [HttpPost("conversations/{conversationId:guid}/messages")]
    [EnableRateLimiting("UserActions")]
    public async Task<ActionResult<MessageDto>> Send(Guid conversationId, SendMessageRequest request, CancellationToken ct)
    {
        var result = await _messaging.SendAsync(CurrentUserId(), conversationId, request.Body, ct);
        return result.Outcome == ServiceOutcome.Ok ? Ok(result.Value) : ToAction(result.Outcome, result.Message);
    }

    [HttpPost("conversations")]
    [EnableRateLimiting("UserActions")]
    public async Task<ActionResult<ConversationDto>> Start(StartConversationRequest request, CancellationToken ct)
    {
        var result = await _messaging.StartAsync(CurrentUserId(), request.RecipientUserId, request.Body, ct);
        return result.Outcome == ServiceOutcome.Ok ? Ok(result.Value) : ToAction(result.Outcome, result.Message);
    }

    [HttpPost("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid conversationId, CancellationToken ct) =>
        await _messaging.MarkReadAsync(CurrentUserId(), conversationId, ct) ? NoContent() : NotFound();

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountResponse>> UnreadCount(CancellationToken ct) =>
        Ok(new UnreadCountResponse { Unread = await _messaging.UnreadCountAsync(CurrentUserId(), ct) });

    [HttpGet("contacts")]
    public async Task<ActionResult<IReadOnlyList<ContactDto>>> Contacts(CancellationToken ct) =>
        Ok(await _messaging.GetContactsAsync(CurrentUserId(), ct));

    private ActionResult ToAction(ServiceOutcome outcome, string? message) => outcome switch
    {
        ServiceOutcome.NotFound => NotFound(message is null ? null : new { message }),
        ServiceOutcome.Conflict => Conflict(new { message }),
        _ => BadRequest(new { message }),
    };

    private Guid CurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(sub!);
    }
}
