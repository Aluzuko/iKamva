using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Email;

public sealed record EmailProcessResult(int Claimed, int Sent, int Retrying, int Failed);

public interface IEmailQueueProcessor
{
    Task<EmailProcessResult> ProcessAsync(int batchSize, CancellationToken ct = default);

    Task<int> PurgeAsync(TimeSpan olderThan, CancellationToken ct = default);
}

public class EmailQueueProcessor : IEmailQueueProcessor
{
    public static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan[] Backoff =
        [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1), TimeSpan.FromHours(6)];
    private static readonly string WorkerId = $"{Environment.MachineName}:{Environment.ProcessId}";

    private readonly AppDbContext _db;
    private readonly IEmailTransport _transport;
    private readonly IFileStorage _storage;
    private readonly ILogger<EmailQueueProcessor> _logger;

    public EmailQueueProcessor(AppDbContext db, IEmailTransport transport, IFileStorage storage, ILogger<EmailQueueProcessor> logger)
    {
        _db = db;
        _transport = transport;
        _storage = storage;
        _logger = logger;
    }

    public async Task<EmailProcessResult> ProcessAsync(int batchSize, CancellationToken ct = default)
    {
        batchSize = Math.Clamp(batchSize, 1, 200);
        var claimed = await ClaimAsync(batchSize, ct);
        int sent = 0, retrying = 0, failed = 0;

        foreach (var message in claimed)
        {
            if (message.Attempts > message.MaxAttempts)
            {
                Fail(message, "Gave up: the sender stopped before finishing and the retry limit was reached.");
                failed++;
                await _db.SaveChangesAsync(CancellationToken.None);
                continue;
            }

            try
            {
                var providerId = await _transport.SendAsync(message, ct);
                message.Status = EmailStatus.Sent;
                message.SentAt = DateTime.UtcNow;
                message.ProviderMessageId = providerId;
                message.LastError = null;
                if (message.IsSensitive)
                {
                    message.HtmlBody = "<p>[Body removed after sending because it contained a private link.]</p>";
                    message.TextBody = null;
                    message.BodyPurged = true;
                }
                sent++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                message.Status = EmailStatus.Pending;
                message.Attempts = Math.Max(0, message.Attempts - 1);
                message.NextAttemptAt = DateTime.UtcNow;
                Unlock(message);
                await _db.SaveChangesAsync(CancellationToken.None);
                throw;
            }
            catch (PermanentEmailFailureException ex)
            {
                Fail(message, ex.Message);
                failed++;
                _logger.LogWarning(ex, "Email {EmailId} failed permanently", message.Id);
            }
            catch (Exception ex)
            {
                var error = Describe(ex);
                if (message.Attempts >= message.MaxAttempts)
                {
                    Fail(message, $"Gave up after {message.Attempts} attempts. Last error: {error}");
                    failed++;
                }
                else
                {
                    message.Status = EmailStatus.Pending;
                    message.LastError = Truncate(error);
                    message.NextAttemptAt = DateTime.UtcNow + Delay(message.Attempts);
                    retrying++;
                }
                _logger.LogWarning(ex, "Email {EmailId} attempt {Attempt} failed", message.Id, message.Attempts);
            }

            Unlock(message);
            message.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(CancellationToken.None);
        }

        if (claimed.Count > 0)
        {
            _logger.LogInformation("Email queue: {Claimed} claimed, {Sent} sent, {Retrying} retrying, {Failed} failed", claimed.Count, sent, retrying, failed);
        }
        return new EmailProcessResult(claimed.Count, sent, retrying, failed);
    }

    public async Task<int> PurgeAsync(TimeSpan olderThan, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var old = await _db.EmailMessages
            .Include(m => m.Attachments)
            .Where(m => (m.Status == EmailStatus.Sent || m.Status == EmailStatus.Cancelled || m.Status == EmailStatus.Failed) && m.UpdatedAt < cutoff)
            .OrderBy(m => m.UpdatedAt)
            .Take(1000)
            .ToListAsync(ct);

        foreach (var attachment in old.SelectMany(m => m.Attachments))
        {
            await _storage.DeleteAsync(attachment.StorageKey, ct);
        }
        _db.EmailMessages.RemoveRange(old);
        await _db.SaveChangesAsync(ct);
        return old.Count;
    }

    private async Task<List<EmailMessage>> ClaimAsync(int batchSize, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var pending = nameof(EmailStatus.Pending);
        var sending = nameof(EmailStatus.Sending);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var rows = await _db.EmailMessages
            .FromSqlInterpolated($@"
                SELECT * FROM ""EmailMessages""
                WHERE (""Status"" = {pending} AND ""NextAttemptAt"" <= {now})
                   OR (""Status"" = {sending} AND ""LockedUntil"" < {now})
                ORDER BY ""Priority"", ""NextAttemptAt""
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED")
            .ToListAsync(ct);

        foreach (var message in rows)
        {
            message.Status = EmailStatus.Sending;
            message.LockedUntil = now + Lease;
            message.LockedBy = WorkerId;
            message.Attempts++;
            message.LastAttemptAt = now;
            message.UpdatedAt = now;
        }
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        if (rows.Count > 0)
        {
            var ids = rows.Select(r => r.Id).ToList();
            await _db.EmailAttachments.Where(a => ids.Contains(a.EmailMessageId)).LoadAsync(ct);
        }
        return rows;
    }

    private static void Fail(EmailMessage message, string error)
    {
        message.Status = EmailStatus.Failed;
        message.LastError = Truncate(error);
        Unlock(message);
    }

    private static void Unlock(EmailMessage message)
    {
        message.LockedUntil = null;
        message.LockedBy = null;
    }

    private static TimeSpan Delay(int attempt)
    {
        var baseDelay = Backoff[Math.Clamp(attempt - 1, 0, Backoff.Length - 1)];
        var jitter = 0.9 + Random.Shared.NextDouble() * 0.2;
        return TimeSpan.FromSeconds(baseDelay.TotalSeconds * jitter);
    }

    private static string Describe(Exception ex) => ex.InnerException is null ? ex.Message : $"{ex.Message} ({ex.InnerException.Message})";

    private static string Truncate(string value) => value.Length <= 2000 ? value : value[..2000];
}
