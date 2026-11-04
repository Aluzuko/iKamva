using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Infrastructure.Payments;
using Backend.Infrastructure.Students;
using Backend.Infrastructure.Tutors;
using Backend.Models.Student;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Backend.Controllers;

[ApiController]
[Route("api/student")]
[Authorize(Roles = "student")]
public class StudentController : ControllerBase
{
    private readonly IStudentService _students;
    private readonly ITutorDirectoryService _directory;
    private readonly IBookingService _bookings;
    private readonly IPayFastService _payFast;

    public StudentController(
        IStudentService students, ITutorDirectoryService directory, IBookingService bookings, IPayFastService payFast)
    {
        _students = students;
        _directory = directory;
        _bookings = bookings;
        _payFast = payFast;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<StudentDashboardResponse>> Dashboard(CancellationToken ct) =>
        Ok(await _students.GetDashboardAsync(CurrentUserId(), ct));

    [HttpGet("subjects")]
    public async Task<ActionResult<IReadOnlyList<SubjectRefDto>>> Subjects(CancellationToken ct) =>
        Ok(await _directory.GetSubjectsAsync(ct));

    [HttpGet("tutors")]
    public async Task<ActionResult<IReadOnlyList<TutorCardDto>>> Tutors(
        [FromQuery] Guid? subjectId, [FromQuery] decimal? maxRate, [FromQuery] decimal? minRating,
        [FromQuery] string? search, [FromQuery] string? sort, CancellationToken ct) =>
        Ok(await _directory.SearchAsync(new TutorSearchQuery
        {
            SubjectId = subjectId,
            MaxRate = maxRate,
            MinRating = minRating,
            Search = search is { Length: > 100 } ? search[..100] : search,
            Sort = sort,
        }, ct));

    [HttpGet("tutors/{tutorId:guid}")]
    public async Task<ActionResult<TutorPublicProfileDto>> Tutor(Guid tutorId, CancellationToken ct)
    {
        var profile = await _directory.GetProfileAsync(tutorId, ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("tutors/{tutorId:guid}/slots")]
    public async Task<ActionResult<SlotsResponse>> Slots(
        Guid tutorId, [FromQuery] DateOnly? from, [FromQuery] int days = 14, [FromQuery] int duration = 60, CancellationToken ct = default)
    {
        var slots = await _directory.GetSlotsAsync(tutorId, CurrentUserId(), from, days, duration, ct);
        return slots is null ? NotFound() : Ok(slots);
    }

    [HttpPost("bookings")]
    [EnableRateLimiting("UserActions")]
    public async Task<ActionResult<BookingCreatedDto>> Book(CreateBookingRequest request, CancellationToken ct)
    {
        var result = await _bookings.CreateAsync(CurrentUserId(), request, ct);
        if (result.Outcome != ServiceOutcome.Ok || result.Value is null)
        {
            return ToAction(new ServiceResult(result.Outcome, result.Message));
        }

        var booking = result.Value;
        if (!booking.PaymentRequired)
        {
            return Ok(booking);
        }

        var checkout = await _payFast.CreateCheckoutAsync(CurrentUserId(), booking.InvoiceId, ct);
        var checkoutStarted = checkout.Outcome == ServiceOutcome.Ok && checkout.Value is not null;
        return Ok(new BookingCreatedDto
        {
            BookingId = booking.BookingId,
            InvoiceId = booking.InvoiceId,
            ScheduledAt = booking.ScheduledAt,
            Price = booking.Price,
            PaymentRequired = true,
            PaymentRedirect = checkoutStarted ? checkout.Value : null,
            PaymentError = checkoutStarted ? null : checkout.Message ?? "Online payment could not be started.",
        });
    }

    [HttpGet("lessons")]
    public async Task<ActionResult<IReadOnlyList<StudentLessonDto>>> Lessons(CancellationToken ct) =>
        Ok(await _students.GetLessonsAsync(CurrentUserId(), ct));

    [HttpPost("lessons/{bookingId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid bookingId, CancelBookingRequest request, CancellationToken ct) =>
        ToAction(await _students.CancelAsync(CurrentUserId(), bookingId, request.Reason, ct));

    [HttpPost("lessons/{bookingId:guid}/review")]
    public async Task<IActionResult> Review(Guid bookingId, CreateReviewRequest request, CancellationToken ct) =>
        ToAction(await _students.ReviewAsync(CurrentUserId(), bookingId, request, ct));

    [HttpGet("billing")]
    public async Task<ActionResult<StudentBillingResponse>> Billing(CancellationToken ct) =>
        Ok(await _students.GetBillingAsync(CurrentUserId(), ct));

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
