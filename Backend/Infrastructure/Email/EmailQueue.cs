using System.Net.Mail;
using Backend.Domain.Entities;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Email;

public interface IEmailQueue
{
    Task<EmailMessage?> StageAsync(EmailRequest request, CancellationToken ct = default);

    Task<EmailMessage?> EnqueueAsync(EmailRequest request, CancellationToken ct = default);
}

public class EmailQueue : IEmailQueue
{
    private static readonly string[] BlockedExtensions =
        [".exe", ".bat", ".cmd", ".com", ".scr", ".js", ".jse", ".vbs", ".vbe", ".ps1", ".msi", ".jar", ".dll", ".lnk", ".hta"];

    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly EmailOptions _options;

    public EmailQueue(AppDbContext db, IFileStorage storage, IOptions<EmailOptions> options)
    {
        _db = db;
        _storage = storage;
        _options = options.Value;
    }

    public async Task<EmailMessage?> StageAsync(EmailRequest request, CancellationToken ct = default)
    {
        Validate(request);

        if (request.IdempotencyKey is not null)
        {
            var tracked = _db.EmailMessages.Local.FirstOrDefault(m => m.IdempotencyKey == request.IdempotencyKey);
            if (tracked is not null || await _db.EmailMessages.AnyAsync(m => m.IdempotencyKey == request.IdempotencyKey, ct))
            {
                return null;
            }
        }

        var now = DateTime.UtcNow;
        var message = new EmailMessage
        {
            Category = request.Category,
            ToEmail = request.ToEmail.Trim(),
            ToName = Clean(request.ToName, 200),
            CcEmails = Join(request.Cc),
            BccEmails = Join(request.Bcc),
            ReplyToEmail = request.ReplyTo?.Trim() ?? _options.ReplyToAddress,
            RecipientUserId = request.RecipientUserId,
            Subject = Clean(request.Content.Subject, 300)!,
            HtmlBody = request.Content.Html,
            TextBody = request.Content.Text,
            Priority = request.Priority,
            IsSensitive = request.IsSensitive,
            RelatedEntityType = request.RelatedEntityType,
            RelatedEntityId = request.RelatedEntityId,
            IdempotencyKey = request.IdempotencyKey,
            CreatedByUserId = request.CreatedByUserId,
            MaxAttempts = Math.Clamp(request.MaxAttempts, 1, 10),
            NextAttemptAt = request.SendAfter is { } after && after > now ? after.ToUniversalTime() : now,
        };

        foreach (var input in request.Attachments)
        {
            var attachment = new EmailAttachment
            {
                EmailMessageId = message.Id,
                FileName = SafeFileName(input.FileName),
                ContentType = string.IsNullOrWhiteSpace(input.ContentType) ? "application/octet-stream" : input.ContentType,
                SizeBytes = input.Content.LongLength,
                StorageKey = string.Empty,
                ContentId = input.ContentId,
                IsInline = input.ContentId is not null,
            };
            attachment.StorageKey = $"email/{now:yyyy/MM}/{message.Id}/{attachment.Id}{Path.GetExtension(attachment.FileName)}";
            await using var stream = new MemoryStream(input.Content, writable: false);
            await _storage.SaveAsync(attachment.StorageKey, stream, attachment.ContentType, ct);
            message.Attachments.Add(attachment);
        }

        _db.EmailMessages.Add(message);
        return message;
    }

    public async Task<EmailMessage?> EnqueueAsync(EmailRequest request, CancellationToken ct = default)
    {
        var message = await StageAsync(request, ct);
        if (message is null)
        {
            return null;
        }
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (request.IdempotencyKey is not null)
        {
            _db.Entry(message).State = EntityState.Detached;
            return null;
        }
        return message;
    }

    private void Validate(EmailRequest request)
    {
        foreach (var address in new[] { request.ToEmail }.Concat(request.Cc).Concat(request.Bcc))
        {
            if (!IsValidAddress(address))
            {
                throw new ArgumentException($"'{address}' isn’t a valid email address.");
            }
        }
        if (request.ReplyTo is not null && !IsValidAddress(request.ReplyTo))
        {
            throw new ArgumentException("The reply-to address isn’t valid.");
        }
        if (string.IsNullOrWhiteSpace(request.Content.Subject))
        {
            throw new ArgumentException("An email needs a subject.");
        }
        if (string.IsNullOrWhiteSpace(request.Content.Html))
        {
            throw new ArgumentException("An email needs a body.");
        }
        if (request.Content.Html.Length > 1_000_000)
        {
            throw new ArgumentException("The email body is too large.");
        }
        if (request.Attachments.Count > _options.MaxAttachments)
        {
            throw new ArgumentException($"An email can have at most {_options.MaxAttachments} attachments.");
        }
        if (request.Attachments.Sum(a => (long)a.Content.Length) > _options.MaxAttachmentBytes)
        {
            throw new ArgumentException($"Attachments must add up to {_options.MaxAttachmentBytes / 1024 / 1024} MB or less.");
        }
        foreach (var a in request.Attachments)
        {
            if (a.Content.Length == 0)
            {
                throw new ArgumentException($"The attachment '{a.FileName}' is empty.");
            }
            if (BlockedExtensions.Contains(Path.GetExtension(a.FileName).ToLowerInvariant()))
            {
                throw new ArgumentException($"'{a.FileName}' can’t be attached because that file type is blocked by email providers.");
            }
        }
    }

    public static bool IsValidAddress(string? address)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Length > 320 || address.Contains(',') || address.Contains(';'))
        {
            return false;
        }
        return MailAddress.TryCreate(address.Trim(), out var parsed) && parsed.Address == address.Trim();
    }

    private static string? Join(IReadOnlyList<string> addresses) =>
        addresses.Count == 0 ? null : string.Join(", ", addresses.Select(a => a.Trim()));

    private static string? Clean(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var text = new string(value.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return text.Length <= max ? text : text[..max];
    }

    private static string SafeFileName(string name)
    {
        var file = Path.GetFileName(name ?? string.Empty);
        file = new string(file.Where(c => !char.IsControl(c) && c is not ('/' or '\\' or '"' or ':' or '*' or '?' or '<' or '>' or '|')).ToArray()).Trim();
        if (string.IsNullOrEmpty(file))
        {
            file = "attachment";
        }
        return file.Length <= 200 ? file : file[^200..];
    }
}
