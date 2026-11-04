using System.ComponentModel.DataAnnotations;
using Backend.Domain.Enums;

namespace Backend.Models.Student;

public sealed class CreateBookingRequest
{
    [Required]
    public required Guid TutorId { get; init; }

    [Required]
    public required Guid SubjectId { get; init; }

    [Required]
    public required DateTime StartUtc { get; init; }

    [Range(30, 180)]
    public required int DurationMinutes { get; init; }
}

public sealed class BookingCreatedDto
{
    public required Guid BookingId { get; init; }
    public required Guid InvoiceId { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public required decimal Price { get; init; }
    public required bool PaymentRequired { get; init; }
    public Backend.Models.Payments.PaymentRedirectResponse? PaymentRedirect { get; init; }
    public string? PaymentError { get; init; }
}

public sealed class StudentLessonDto
{
    public required Guid BookingId { get; init; }
    public required Guid TutorId { get; init; }
    public required string TutorName { get; init; }
    public required string SubjectName { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public required int DurationMinutes { get; init; }
    public required decimal Price { get; init; }
    public required BookingStatus Status { get; init; }
    public string? Notes { get; init; }
    public string? RecordingUrl { get; init; }
    public string? CancellationReason { get; init; }
    public required bool CanCancel { get; init; }
    public required bool CanReview { get; init; }
    public int? MyRating { get; init; }
    public string? MyComment { get; init; }
}

public sealed class CancelBookingRequest
{
    [Required, MinLength(5), MaxLength(500)]
    public required string Reason { get; init; }
}

public sealed class CreateReviewRequest
{
    [Range(1, 5)]
    public required int Rating { get; init; }

    [MaxLength(1000)]
    public string? Comment { get; init; }
}

public sealed class LinkedParentDto
{
    public required string FullName { get; init; }
    public required Relationship Relationship { get; init; }
}

public sealed class StudentDashboardResponse
{
    public StudentLessonDto? NextLesson { get; init; }
    public required IReadOnlyList<StudentLessonDto> UpcomingLessons { get; init; }
    public required int UpcomingCount { get; init; }
    public required int CompletedCount { get; init; }
    public required decimal HoursThisMonth { get; init; }
    public required int LessonsToReview { get; init; }
    public required IReadOnlyList<SubjectProgressDto> Subjects { get; init; }
    public required string LinkCode { get; init; }
    public required IReadOnlyList<LinkedParentDto> Parents { get; init; }
}

public sealed class SubjectProgressDto
{
    public required string SubjectName { get; init; }
    public required int CompletedLessons { get; init; }
    public required decimal Hours { get; init; }
    public DateTime? LastLessonAt { get; init; }
}

public sealed class StudentInvoiceDto
{
    public required Guid InvoiceId { get; init; }
    public required string PeriodDescription { get; init; }
    public required decimal Amount { get; init; }
    public required InvoiceStatus Status { get; init; }
    public DateTime? PaidAt { get; init; }
}

public sealed class ChargeDto
{
    public required Guid BookingId { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public required string TutorName { get; init; }
    public required string SubjectName { get; init; }
    public required int DurationMinutes { get; init; }
    public required decimal Price { get; init; }
    public required BookingStatus Status { get; init; }
}

public sealed class StudentBillingResponse
{
    public required bool PaidByParent { get; init; }
    public required IReadOnlyList<LinkedParentDto> Parents { get; init; }
    public required string LinkCode { get; init; }
    public required decimal Outstanding { get; init; }
    public required decimal ThisMonthTotal { get; init; }
    public required decimal UpcomingTotal { get; init; }
    public required IReadOnlyList<ChargeDto> ThisMonth { get; init; }
    public required IReadOnlyList<StudentInvoiceDto> Invoices { get; init; }
}
