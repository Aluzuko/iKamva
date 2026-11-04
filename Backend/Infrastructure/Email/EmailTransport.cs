using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Backend.Domain.Entities;
using Backend.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Email;

public sealed class PermanentEmailFailureException : Exception
{
    public PermanentEmailFailureException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

public interface IEmailTransport
{
    Task<string> SendAsync(EmailMessage message, CancellationToken ct = default);
}

public class SmtpEmailTransport : IEmailTransport
{
    private readonly EmailOptions _options;
    private readonly IFileStorage _storage;
    private readonly string _pickupPath;

    public SmtpEmailTransport(IOptions<EmailOptions> options, IFileStorage storage, IWebHostEnvironment env)
    {
        _options = options.Value;
        _storage = storage;
        _pickupPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, _options.PickupDirectory));
    }

    public async Task<string> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var streams = new List<Stream>();
        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(message.FromEmail ?? _options.FromAddress, message.FromName ?? _options.FromName, Encoding.UTF8),
                Subject = message.Subject,
                SubjectEncoding = Encoding.UTF8,
                HeadersEncoding = Encoding.UTF8,
                BodyEncoding = Encoding.UTF8,
            };

            try
            {
                mail.To.Add(new MailAddress(message.ToEmail, message.ToName, Encoding.UTF8));
                AddAll(mail.CC, message.CcEmails);
                AddAll(mail.Bcc, message.BccEmails);
                if (!string.IsNullOrWhiteSpace(message.ReplyToEmail))
                {
                    mail.ReplyToList.Add(new MailAddress(message.ReplyToEmail));
                }
            }
            catch (FormatException ex)
            {
                throw new PermanentEmailFailureException("An address on this email isn’t valid.", ex);
            }

            mail.Headers.Add("X-iKamva-Email-Id", message.Id.ToString());
            mail.Headers.Add("X-iKamva-Category", message.Category.ToString());

            var html = AlternateView.CreateAlternateViewFromString(message.HtmlBody, Encoding.UTF8, MediaTypeNames.Text.Html);
            if (!string.IsNullOrEmpty(message.TextBody))
            {
                mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.TextBody, Encoding.UTF8, MediaTypeNames.Text.Plain));
            }
            mail.AlternateViews.Add(html);

            foreach (var attachment in message.Attachments)
            {
                var file = await _storage.OpenReadAsync(attachment.StorageKey, ct)
                    ?? throw new PermanentEmailFailureException($"The attachment '{attachment.FileName}' is missing from storage.");
                streams.Add(file.Content);

                if (attachment.IsInline && attachment.ContentId is not null)
                {
                    html.LinkedResources.Add(new LinkedResource(file.Content, attachment.ContentType) { ContentId = attachment.ContentId });
                }
                else
                {
                    var part = new Attachment(file.Content, attachment.FileName, attachment.ContentType);
                    part.ContentDisposition!.FileName = attachment.FileName;
                    part.NameEncoding = Encoding.UTF8;
                    mail.Attachments.Add(part);
                }
            }

            using var client = CreateClient();
            try
            {
                await client.SendMailAsync(mail, ct);
            }
            catch (SmtpFailedRecipientException ex) when (IsPermanent(ex.StatusCode))
            {
                throw new PermanentEmailFailureException($"The mail server rejected the recipient ({ex.StatusCode}).", ex);
            }
            catch (SmtpException ex) when (IsPermanent(ex.StatusCode))
            {
                throw new PermanentEmailFailureException($"The mail server refused this email ({ex.StatusCode}).", ex);
            }

            return message.Id.ToString();
        }
        finally
        {
            foreach (var s in streams)
            {
                await s.DisposeAsync();
            }
        }
    }

    private SmtpClient CreateClient()
    {
        if (string.Equals(_options.Transport, "Pickup", StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(_pickupPath);
            return new SmtpClient { DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory, PickupDirectoryLocation = _pickupPath };
        }

        if (string.IsNullOrWhiteSpace(_options.Smtp.Host))
        {
            throw new InvalidOperationException("Email:Smtp:Host isn’t configured.");
        }

        var client = new SmtpClient(_options.Smtp.Host, _options.Smtp.Port)
        {
            EnableSsl = _options.Smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = _options.Smtp.TimeoutSeconds * 1000,
        };
        if (!string.IsNullOrEmpty(_options.Smtp.Username))
        {
            client.Credentials = new NetworkCredential(_options.Smtp.Username, _options.Smtp.Password);
        }
        return client;
    }

    private static void AddAll(MailAddressCollection collection, string? addresses)
    {
        if (string.IsNullOrWhiteSpace(addresses))
        {
            return;
        }
        foreach (var address in addresses.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            collection.Add(new MailAddress(address));
        }
    }

    private static bool IsPermanent(SmtpStatusCode code) => code is
        SmtpStatusCode.MailboxUnavailable or
        SmtpStatusCode.MailboxNameNotAllowed or
        SmtpStatusCode.UserNotLocalTryAlternatePath or
        SmtpStatusCode.ExceededStorageAllocation or
        SmtpStatusCode.TransactionFailed;
}
