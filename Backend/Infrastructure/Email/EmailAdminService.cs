using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Storage;
using Backend.Infrastructure.Tutors;
using Backend.Models.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Email;

public interface IEmailAdminService
{
    Task<EmailPageResponse> ListAsync(EmailStatus? status, EmailCategory? category, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<EmailStatsResponse> StatsAsync(CancellationToken ct = default);
    Task<EmailDetailDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<(StoredFile File, string FileName)?> OpenAttachmentAsync(Guid emailId, Guid attachmentId, CancellationToken ct = default);
    Task<ServiceResult> RetryAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResult> CancelAsync(Guid id, CancellationToken ct = default);
    Task<ServiceResult<Guid>> ComposeLetterAsync(Guid adminId, ComposeLetterForm form, CancellationToken ct = default);
    Task<ServiceResult<Guid>> QueueInvoiceAsync(Guid adminId, Guid invoiceId, CancellationToken ct = default);
}

public class EmailAdminService : IEmailAdminService
{
    private static readonly string[] MonthNames =
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    private readonly AppDbContext _db;
    private readonly IEmailQueue _queue;
    private readonly IEmailTemplates _templates;
    private readonly IFileStorage _storage;
    private readonly EmailOptions _options;

    public EmailAdminService(AppDbContext db, IEmailQueue queue, IEmailTemplates templates, IFileStorage storage, IOptions<EmailOptions> options)
    {
        _db = db;
        _queue = queue;
        _templates = templates;
        _storage = storage;
        _options = options.Value;
    }

    public async Task<EmailPageResponse> ListAsync(EmailStatus? status, EmailCategory? category, string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.EmailMessages.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(m => m.Status == status);
        }
        if (category is not null)
        {
            query = query.Where(m => m.Category == category);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m => m.ToEmail.ToLower().Contains(term) || m.Subject.ToLower().Contains(term) || (m.ToName != null && m.ToName.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new EmailListItemDto
            {
                EmailId = m.Id,
                Category = m.Category,
                Status = m.Status,
                Priority = m.Priority,
                ToEmail = m.ToEmail,
                ToName = m.ToName,
                Subject = m.Subject,
                Attempts = m.Attempts,
                MaxAttempts = m.MaxAttempts,
                AttachmentCount = m.Attachments.Count,
                CreatedAt = m.CreatedAt,
                NextAttemptAt = m.NextAttemptAt,
                SentAt = m.SentAt,
                LastError = m.LastError,
            })
            .ToListAsync(ct);

        return new EmailPageResponse { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    public async Task<EmailStatsResponse> StatsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var counts = await _db.EmailMessages
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        int Count(EmailStatus s) => counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;

        var due = _db.EmailMessages.Where(m => m.Status == EmailStatus.Pending && m.NextAttemptAt <= now);
        return new EmailStatsResponse
        {
            Pending = Count(EmailStatus.Pending),
            Due = await due.CountAsync(ct),
            Sending = Count(EmailStatus.Sending),
            Sent = Count(EmailStatus.Sent),
            Failed = Count(EmailStatus.Failed),
            Cancelled = Count(EmailStatus.Cancelled),
            OldestDueAt = await due.OrderBy(m => m.NextAttemptAt).Select(m => (DateTime?)m.NextAttemptAt).FirstOrDefaultAsync(ct),
            WorkerEnabled = _options.Worker.Enabled,
            Transport = _options.Transport,
        };
    }

    public async Task<EmailDetailDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var m = await _db.EmailMessages.AsNoTracking().Include(x => x.Attachments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null)
        {
            return null;
        }
        return new EmailDetailDto
        {
            Summary = new EmailListItemDto
            {
                EmailId = m.Id, Category = m.Category, Status = m.Status, Priority = m.Priority, ToEmail = m.ToEmail, ToName = m.ToName,
                Subject = m.Subject, Attempts = m.Attempts, MaxAttempts = m.MaxAttempts, AttachmentCount = m.Attachments.Count,
                CreatedAt = m.CreatedAt, NextAttemptAt = m.NextAttemptAt, SentAt = m.SentAt, LastError = m.LastError,
            },
            CcEmails = m.CcEmails,
            BccEmails = m.BccEmails,
            ReplyToEmail = m.ReplyToEmail,
            HtmlBody = m.IsSensitive && !m.BodyPurged ? "<p>[Hidden: this email contains a private link.]</p>" : m.HtmlBody,
            TextBody = m.IsSensitive ? null : m.TextBody,
            IsSensitive = m.IsSensitive,
            BodyPurged = m.BodyPurged,
            RelatedEntityType = m.RelatedEntityType,
            RelatedEntityId = m.RelatedEntityId,
            LastAttemptAt = m.LastAttemptAt,
            ProviderMessageId = m.ProviderMessageId,
            Attachments = m.Attachments.OrderBy(a => a.CreatedAt).Select(a => new EmailAttachmentDto
            {
                AttachmentId = a.Id, FileName = a.FileName, ContentType = a.ContentType, SizeBytes = a.SizeBytes, IsInline = a.IsInline,
            }).ToList(),
        };
    }

    public async Task<(StoredFile File, string FileName)?> OpenAttachmentAsync(Guid emailId, Guid attachmentId, CancellationToken ct = default)
    {
        var attachment = await _db.EmailAttachments.FirstOrDefaultAsync(a => a.Id == attachmentId && a.EmailMessageId == emailId, ct);
        if (attachment is null)
        {
            return null;
        }
        var file = await _storage.OpenReadAsync(attachment.StorageKey, ct);
        return file is null ? null : (file with { ContentType = attachment.ContentType }, attachment.FileName);
    }

    public async Task<ServiceResult> RetryAsync(Guid id, CancellationToken ct = default)
    {
        var m = await _db.EmailMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null)
        {
            return ServiceResult.NotFound();
        }
        if (m.Status is not (EmailStatus.Failed or EmailStatus.Cancelled or EmailStatus.Pending))
        {
            return ServiceResult.Conflict(m.Status == EmailStatus.Sent ? "This email was already sent." : "This email is being sent right now.");
        }
        if (m.BodyPurged)
        {
            return ServiceResult.Conflict("This email’s content was removed after sending, so it can’t be sent again.");
        }
        m.Status = EmailStatus.Pending;
        m.Attempts = 0;
        m.LastError = null;
        m.NextAttemptAt = DateTime.UtcNow;
        m.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var m = await _db.EmailMessages.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null)
        {
            return ServiceResult.NotFound();
        }
        if (m.Status != EmailStatus.Pending)
        {
            return ServiceResult.Conflict("Only emails waiting to be sent can be cancelled.");
        }
        m.Status = EmailStatus.Cancelled;
        m.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<Guid>> ComposeLetterAsync(Guid adminId, ComposeLetterForm form, CancellationToken ct = default)
    {
        var admin = await _db.Users.FirstAsync(u => u.Id == adminId, ct);
        var attachments = new List<EmailAttachmentInput>();
        foreach (var file in form.Files)
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, ct);
            attachments.Add(new EmailAttachmentInput(file.FileName, string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType, stream.ToArray()));
        }

        var toEmail = form.ToEmail!.Trim().ToLowerInvariant();
        var recipient = await _db.Users.FirstOrDefaultAsync(u => u.Email == toEmail, ct);
        try
        {
            var message = await _queue.EnqueueAsync(new EmailRequest
            {
                Category = EmailCategory.Letter,
                ToEmail = toEmail,
                ToName = form.ToName ?? recipient?.FullName,
                RecipientUserId = recipient?.Id,
                Content = _templates.Letter(form.ToName ?? recipient?.FullName ?? string.Empty, form.Subject!.Trim(), form.Message!.Trim(), admin.FullName),
                ReplyTo = EmailQueue.IsValidAddress(admin.Email) ? admin.Email : null,
                CreatedByUserId = adminId,
                Attachments = attachments,
            }, ct);
            return ServiceResult<Guid>.Ok(message!.Id);
        }
        catch (ArgumentException ex)
        {
            return ServiceResult<Guid>.From(ServiceResult.Invalid(ex.Message));
        }
    }

    public async Task<ServiceResult<Guid>> QueueInvoiceAsync(Guid adminId, Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice is null)
        {
            return ServiceResult<Guid>.From(ServiceResult.NotFound());
        }
        var payerId = invoice.ParentUserId ?? invoice.StudentUserId;
        var people = await _db.Users.Where(u => u.Id == payerId || u.Id == invoice.StudentUserId).ToDictionaryAsync(u => u.Id, ct);
        if (!people.TryGetValue(payerId, out var payer))
        {
            return ServiceResult<Guid>.From(ServiceResult.Conflict("The invoice payer account could not be found."));
        }
        var studentName = people.TryGetValue(invoice.StudentUserId, out var student) ? student.FullName : "the student";

        var period = $"{MonthNames[invoice.Month - 1]} {invoice.Year}";
        var monthStart = TutorRules.LocalToUtc(new DateTime(invoice.Year, invoice.Month, 1));
        var monthEnd = TutorRules.LocalToUtc(new DateTime(invoice.Year, invoice.Month, 1).AddMonths(1));
        var lines = await _db.Bookings
            .Where(b => invoice.BookingId != null
                ? b.Id == invoice.BookingId
                : b.StudentUserId == invoice.StudentUserId
                    && b.Status == BookingStatus.Completed
                    && b.ScheduledAt >= monthStart
                    && b.ScheduledAt < monthEnd)
            .OrderBy(b => b.ScheduledAt)
            .Select(b => new InvoiceLine(b.ScheduledAt, b.Subject.Name + " - " + b.DurationMinutes + " min", b.PriceAmount))
            .ToListAsync(ct);
        if (lines.Count == 0)
        {
            lines.Add(new InvoiceLine(invoice.IssuedAt, $"{invoice.LessonCount} lessons", invoice.AmountTotal));
        }

        var number = $"INV-{invoice.Year}{invoice.Month:00}-{invoice.Id.ToString()[..8].ToUpperInvariant()}";
        var paid = invoice.Status == InvoiceStatus.Paid;
        var pdf = SimplePdf.Invoice(number, payer.FullName, payer.Email, studentName, period, lines, invoice.AmountTotal, paid, invoice.IssuedAt);

        var message = await _queue.EnqueueAsync(new EmailRequest
        {
            Category = EmailCategory.Invoice,
            ToEmail = payer.Email,
            ToName = payer.FullName,
            RecipientUserId = payer.Id,
            Content = _templates.Invoice(payer.FullName, studentName, period, invoice.AmountTotal, paid),
            RelatedEntityType = "Invoice",
            RelatedEntityId = invoice.Id,
            CreatedByUserId = adminId,
            Attachments = [new EmailAttachmentInput($"{number}.pdf", "application/pdf", pdf)],
        }, ct);
        return ServiceResult<Guid>.Ok(message!.Id);
    }
}
