using System.Globalization;
using System.Net;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Data;
using Backend.Infrastructure.Email;
using Backend.Infrastructure.Tutors;
using Backend.Models.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Payments;

public interface IPayFastService
{
    bool IsConfigured { get; }

    Task<ServiceResult<PaymentRedirectResponse>> CreateCheckoutAsync(Guid payerId, Guid invoiceId, CancellationToken ct = default);

    Task<PaymentAttemptDto?> GetAttemptAsync(Guid payerId, Guid attemptId, CancellationToken ct = default);

    Task MarkCancelledAsync(Guid attemptId, CancellationToken ct = default);

    Task<bool> HandleNotificationAsync(string rawBody, IPAddress? sourceIp, CancellationToken ct = default);

    Task<string> ClientResultUrlAsync(Guid? attemptId, bool cancelled, CancellationToken ct = default);
}

public class PayFastService : IPayFastService
{
    private const string Provider = "PayFast";
    private static readonly string[] ValidHosts = ["www.payfast.co.za", "sandbox.payfast.co.za", "w1w.payfast.co.za", "w2w.payfast.co.za"];
    private static readonly string[] MonthNames =
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly PayFastOptions _options;
    private readonly ILogger<PayFastService> _logger;
    private readonly IEmailQueue _emails;
    private readonly IEmailTemplates _templates;

    public PayFastService(
        AppDbContext db, HttpClient http, IOptions<PayFastOptions> options, ILogger<PayFastService> logger,
        IEmailQueue emails, IEmailTemplates templates)
    {
        _db = db;
        _http = http;
        _options = options.Value;
        _logger = logger;
        _emails = emails;
        _templates = templates;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<ServiceResult<PaymentRedirectResponse>> CreateCheckoutAsync(Guid payerId, Guid invoiceId, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
        {
            return ServiceResult<PaymentRedirectResponse>.From(ServiceResult.Conflict("Online payments aren’t set up yet. Please try again later."));
        }

        var invoice = await _db.Invoices.FirstOrDefaultAsync(
            i => i.Id == invoiceId && (i.ParentUserId == payerId || (i.ParentUserId == null && i.StudentUserId == payerId)), ct);
        if (invoice is null)
        {
            return ServiceResult<PaymentRedirectResponse>.From(ServiceResult.NotFound());
        }
        if (invoice.Status == InvoiceStatus.Paid)
        {
            return ServiceResult<PaymentRedirectResponse>.From(ServiceResult.Conflict("This invoice has already been paid."));
        }
        if (invoice.AmountTotal <= 0)
        {
            return ServiceResult<PaymentRedirectResponse>.From(ServiceResult.Invalid("There’s nothing to pay on this invoice."));
        }

        var payer = await _db.Users.FirstOrDefaultAsync(u => u.Id == payerId, ct);
        if (payer is null)
        {
            return ServiceResult<PaymentRedirectResponse>.From(ServiceResult.NotFound());
        }
        var student = await _db.Users.Where(u => u.Id == invoice.StudentUserId).Select(u => u.FullName).FirstOrDefaultAsync(ct) ?? "student";

        var attempt = new PaymentAttempt
        {
            InvoiceId = invoice.Id,
            PayerUserId = payerId,
            Amount = invoice.AmountTotal,
            Provider = Provider,
        };
        _db.PaymentAttempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        var names = payer.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var fields = new List<KeyValuePair<string, string>>
        {
            new("merchant_id", _options.MerchantId),
            new("merchant_key", _options.MerchantKey),
            new("return_url", WithAttempt(_options.ReturnUrl ?? ApiCallbackUrl("return"), attempt.Id)),
            new("cancel_url", WithAttempt(_options.CancelUrl ?? ApiCallbackUrl("cancel"), attempt.Id)),
            new("notify_url", _options.NotifyUrl ?? ApiCallbackUrl("notify")),
            new("name_first", Truncate(names.ElementAtOrDefault(0) ?? string.Empty, 100)),
            new("name_last", Truncate(names.ElementAtOrDefault(1) ?? string.Empty, 100)),
            new("email_address", Truncate(payer.Email, 100)),
            new("m_payment_id", attempt.Id.ToString()),
            new("amount", attempt.Amount.ToString("0.00", CultureInfo.InvariantCulture)),
            new("item_name", Truncate($"iKamva lessons {MonthNames[invoice.Month - 1]} {invoice.Year}", 100)),
            new("item_description", Truncate($"{invoice.LessonCount} lessons for {student}", 255)),
            new("custom_str1", invoice.Id.ToString()),
        };
        fields = fields.Where(f => !string.IsNullOrWhiteSpace(f.Value)).Select(f => new KeyValuePair<string, string>(f.Key, f.Value.Trim())).ToList();
        fields.Add(new("signature", PayFastSignature.ForCheckout(fields, _options.Passphrase)));

        return ServiceResult<PaymentRedirectResponse>.Ok(new PaymentRedirectResponse
        {
            AttemptId = attempt.Id,
            ProcessUrl = _options.ProcessUrl,
            Fields = fields.Select(f => new PaymentField { Name = f.Key, Value = f.Value }).ToList(),
        });
    }

    public async Task<PaymentAttemptDto?> GetAttemptAsync(Guid payerId, Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await _db.PaymentAttempts
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == attemptId && p.PayerUserId == payerId, ct);
        if (attempt is null)
        {
            return null;
        }

        return new PaymentAttemptDto
        {
            AttemptId = attempt.Id,
            InvoiceId = attempt.InvoiceId,
            Status = attempt.Status,
            InvoiceStatus = attempt.Invoice.Status,
            Amount = attempt.Amount,
            Description = $"{MonthNames[attempt.Invoice.Month - 1]} {attempt.Invoice.Year} · {attempt.Invoice.LessonCount} lessons",
            CreatedAt = attempt.CreatedAt,
            CompletedAt = attempt.CompletedAt,
        };
    }

    public async Task MarkCancelledAsync(Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await _db.PaymentAttempts.FirstOrDefaultAsync(p => p.Id == attemptId, ct);
        if (attempt is { Status: PaymentStatus.Pending })
        {
            attempt.Status = PaymentStatus.Cancelled;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> HandleNotificationAsync(string rawBody, IPAddress? sourceIp, CancellationToken ct = default)
    {
        var fields = PayFastSignature.ParseForm(rawBody);
        string? Get(string key) => fields.FirstOrDefault(f => f.Key == key).Value;

        var signed = fields.TakeWhile(f => f.Key != "signature").ToList();
        var expected = PayFastSignature.Hash(signed, _options.Passphrase);
        if (!PayFastSignature.Matches(expected, Get("signature")))
        {
            _logger.LogWarning("PayFast ITN rejected: signature mismatch");
            return false;
        }

        if (_options.VerifySourceIp && !await IsPayFastAddressAsync(sourceIp))
        {
            _logger.LogWarning("PayFast ITN rejected: unexpected source address {Ip}", sourceIp);
            return false;
        }

        if (Get("merchant_id") != _options.MerchantId)
        {
            _logger.LogWarning("PayFast ITN rejected: merchant id mismatch");
            return false;
        }

        if (!Guid.TryParse(Get("m_payment_id"), out var attemptId))
        {
            _logger.LogWarning("PayFast ITN rejected: missing m_payment_id");
            return false;
        }

        var attempt = await _db.PaymentAttempts.Include(p => p.Invoice).FirstOrDefaultAsync(p => p.Id == attemptId, ct);
        if (attempt is null)
        {
            _logger.LogWarning("PayFast ITN rejected: unknown attempt {AttemptId}", attemptId);
            return false;
        }

        if (!decimal.TryParse(Get("amount_gross"), NumberStyles.Number, CultureInfo.InvariantCulture, out var gross)
            || Math.Abs(gross - attempt.Amount) > 0.01m)
        {
            _logger.LogWarning("PayFast ITN rejected: amount {Gross} does not match {Expected} for {AttemptId}", Get("amount_gross"), attempt.Amount, attemptId);
            return false;
        }

        if (!await ConfirmWithPayFastAsync(PayFastSignature.ParamString(signed), ct))
        {
            _logger.LogWarning("PayFast ITN rejected: server confirmation failed for {AttemptId}", attemptId);
            return false;
        }

        var status = Get("payment_status")?.ToUpperInvariant();
        if (status is not ("COMPLETE" or "FAILED" or "CANCELLED" or "PENDING"))
        {
            _logger.LogWarning("PayFast ITN rejected: unsupported payment status {Status} for {AttemptId}", status, attemptId);
            return false;
        }

        if (attempt.Status == PaymentStatus.Complete)
        {
            return true;
        }

        attempt.ProviderStatus = status;
        attempt.ProviderPaymentId = Get("pf_payment_id");
        if (decimal.TryParse(Get("amount_fee"), NumberStyles.Number, CultureInfo.InvariantCulture, out var fee))
        {
            attempt.AmountFee = fee;
        }

        switch (status)
        {
            case "COMPLETE":
                attempt.Status = PaymentStatus.Complete;
                attempt.CompletedAt = DateTime.UtcNow;
                if (attempt.Invoice.Status != InvoiceStatus.Paid)
                {
                    attempt.Invoice.Status = InvoiceStatus.Paid;
                    attempt.Invoice.PaidAt = DateTime.UtcNow;
                }
                await StageConfirmationAsync(attempt, ct);
                break;
            case "FAILED":
                attempt.Status = PaymentStatus.Failed;
                break;
            case "CANCELLED":
                attempt.Status = PaymentStatus.Cancelled;
                break;
            case "PENDING":
                break;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("PayFast ITN {Status} for attempt {AttemptId} (pf {PfId})", status, attemptId, attempt.ProviderPaymentId);
        return true;
    }

    private async Task StageConfirmationAsync(PaymentAttempt attempt, CancellationToken ct)
    {
        var payer = await _db.Users.FirstOrDefaultAsync(u => u.Id == attempt.PayerUserId, ct);
        if (payer is null || !EmailQueue.IsValidAddress(payer.Email))
        {
            return;
        }
        var period = $"{MonthNames[attempt.Invoice.Month - 1]} {attempt.Invoice.Year}";
        await _emails.StageAsync(new EmailRequest
        {
            Category = EmailCategory.PaymentConfirmation,
            ToEmail = payer.Email,
            ToName = payer.FullName,
            RecipientUserId = payer.Id,
            Priority = EmailPriority.High,
            Content = _templates.PaymentConfirmation(payer.FullName, period, attempt.Amount, attempt.ProviderPaymentId ?? attempt.Id.ToString(), attempt.CompletedAt ?? DateTime.UtcNow),
            RelatedEntityType = "PaymentAttempt",
            RelatedEntityId = attempt.Id,
            IdempotencyKey = $"payment-confirmation:{attempt.Id}",
        }, ct);
    }

    public async Task<string> ClientResultUrlAsync(Guid? attemptId, bool cancelled, CancellationToken ct = default)
    {
        var role = attemptId is null
            ? null
            : await (
                from attempt in _db.PaymentAttempts
                join payer in _db.Users on attempt.PayerUserId equals payer.Id
                where attempt.Id == attemptId
                select (UserRole?)payer.Role
            ).FirstOrDefaultAsync(ct);
        var route = role == UserRole.Student ? "student" : "parent";
        var url = $"{_options.ClientUrl.TrimEnd('/')}/{route}/payments/result";
        var query = new List<string>();
        if (attemptId is not null)
        {
            query.Add("attempt=" + attemptId);
        }
        if (cancelled)
        {
            query.Add("cancelled=1");
        }
        return query.Count == 0 ? url : url + "?" + string.Join('&', query);
    }

    private string ApiCallbackUrl(string action)
    {
        var builder = new UriBuilder(_options.PublicApiUrl);
        var path = builder.Path.TrimEnd('/');
        if (!path.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            path += "/api";
        }

        builder.Path = $"{path}/payments/payfast/{action}";
        builder.Query = string.Empty;
        builder.Fragment = string.Empty;
        return builder.Uri.AbsoluteUri;
    }

    private async Task<bool> ConfirmWithPayFastAsync(string paramString, CancellationToken ct)
    {
        try
        {
            using var content = new StringContent(paramString, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
            using var response = await _http.PostAsync(_options.ValidateUrl, content, ct);
            var body = (await response.Content.ReadAsStringAsync(ct)).Trim();
            return response.IsSuccessStatusCode && body == "VALID";
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "PayFast server confirmation could not be reached");
            return false;
        }
    }

    private static async Task<bool> IsPayFastAddressAsync(IPAddress? source)
    {
        if (source is null)
        {
            return false;
        }
        var ip = source.IsIPv4MappedToIPv6 ? source.MapToIPv4() : source;
        foreach (var host in ValidHosts)
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                if (addresses.Any(a => (a.IsIPv4MappedToIPv6 ? a.MapToIPv4() : a).Equals(ip)))
                {
                    return true;
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
            }
        }
        return false;
    }

    private static string WithAttempt(string url, Guid attemptId)
    {
        var builder = new UriBuilder(url);
        var query = builder.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !part.StartsWith("attempt=", StringComparison.OrdinalIgnoreCase))
            .Append("attempt=" + Uri.EscapeDataString(attemptId.ToString()));
        builder.Query = string.Join('&', query);
        return builder.Uri.AbsoluteUri;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
