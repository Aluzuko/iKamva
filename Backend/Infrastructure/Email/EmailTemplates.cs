using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Email;

public sealed record InvoiceLine(DateTime Date, string Description, decimal Amount);

public interface IEmailTemplates
{
    RenderedEmail Welcome(string fullName, string role);
    RenderedEmail AccountVerification(string fullName, string verifyUrl, TimeSpan validFor);
    RenderedEmail PasswordReset(string fullName, string resetUrl, TimeSpan validFor);
    RenderedEmail StudentInvite(string parentName, string inviteeEmail);
    RenderedEmail Invoice(string parentName, string studentName, string period, decimal amount, bool paid);
    RenderedEmail PaymentConfirmation(string parentName, string period, decimal amount, string reference, DateTime paidAt);
    RenderedEmail TutorApproved(string fullName);
    RenderedEmail TutorChangesNeeded(string fullName, string reason);
    RenderedEmail Letter(string recipientName, string subject, string message, string senderName);
    string Url(string path);
}

public class EmailTemplates : IEmailTemplates
{
    private static readonly CultureInfo Money = CultureInfo.InvariantCulture;
    private readonly EmailOptions _options;

    public EmailTemplates(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public string Url(string path) => $"{(_options.ClientUrl is { Length: > 0 } c ? c : "http://localhost:4200").TrimEnd('/')}/{path.TrimStart('/')}";

    public RenderedEmail Welcome(string fullName, string role)
    {
        var next = role switch
        {
            "tutor" => ("Finish your tutor profile", "profile", "Add your subjects and rate, upload your documents and submit them for review. Students can book you once you’re verified."),
            "parent" => ("Link your child", "link-student", "Link your child’s account with their link code or email to follow their lessons and pay for them."),
            _ => ("Find a tutor", "find-tutor", "Browse verified tutors, check their reviews and book a time that suits you."),
        };
        return Build(
            "Welcome to iKamva",
            $"Hi {First(fullName)},",
            [ "Your iKamva account is ready.", next.Item3 ],
            (next.Item1, Url($"{role}/{next.Item2}")),
            "You’re getting this email because an account was created with this address.");
    }

    public RenderedEmail AccountVerification(string fullName, string verifyUrl, TimeSpan validFor) => Build(
        "Confirm your email address",
        $"Hi {First(fullName)},",
        [ "Please confirm that this is your email address so we can keep your account secure.", $"This link expires in {Duration(validFor)}." ],
        ("Confirm email", verifyUrl),
        "If you didn’t create an iKamva account, you can ignore this email.");

    public RenderedEmail PasswordReset(string fullName, string resetUrl, TimeSpan validFor) => Build(
        "Reset your iKamva password",
        $"Hi {First(fullName)},",
        [ "We received a request to reset your password.", $"This link expires in {Duration(validFor)} and can only be used once." ],
        ("Choose a new password", resetUrl),
        "If you didn’t ask for this, you can ignore this email. Your password won’t change.");

    public RenderedEmail StudentInvite(string parentName, string inviteeEmail) => Build(
        $"{parentName} invited you to iKamva",
        "Hi,",
        [ $"{parentName} wants to link your iKamva account to theirs so they can follow your lessons and pay for them.", "Create a student account with this email address and the link is made for you automatically." ],
        ("Create your account", Url($"register/student?email={Uri.EscapeDataString(inviteeEmail)}")),
        "If you don’t know this person, you can ignore this email.");

    public RenderedEmail Invoice(string parentName, string studentName, string period, decimal amount, bool paid) => Build(
        paid ? $"Your iKamva invoice for {period} (paid)" : $"Your iKamva invoice for {period}",
        $"Hi {First(parentName)},",
        [ $"Your invoice for {studentName}’s lessons in {period} is attached.", paid ? $"Amount: {Rand(amount)}. This invoice has been paid, thank you." : $"Amount due: {Rand(amount)}." ],
        paid ? null : ("Pay now", Url("parent/payments")),
        "Questions about this invoice? Reply to this email.");

    public RenderedEmail PaymentConfirmation(string parentName, string period, decimal amount, string reference, DateTime paidAt) => Build(
        $"Payment received: {Rand(amount)}",
        $"Hi {First(parentName)},",
        [ $"We’ve received your payment of {Rand(amount)} for {period}.", $"PayFast reference: {reference}", $"Paid on {paidAt.AddHours(2):d MMMM yyyy, HH:mm} (SAST)." ],
        ("View payments", Url("parent/payments")),
        "Keep this email as your receipt.");

    public RenderedEmail TutorApproved(string fullName) => Build(
        "You’re verified on iKamva",
        $"Hi {First(fullName)},",
        [ "Your documents have been checked and your profile is verified. Students can now find and book you.", "Make sure your weekly availability is up to date." ],
        ("Set your availability", Url("tutor/availability")),
        null);

    public RenderedEmail TutorChangesNeeded(string fullName, string reason) => Build(
        "Your iKamva verification needs changes",
        $"Hi {First(fullName)},",
        [ "We checked your documents and need you to fix something before we can verify you:", $"“{reason}”", "Update your documents and submit them again." ],
        ("Update verification", Url("tutor/profile")),
        null);

    public RenderedEmail Letter(string recipientName, string subject, string message, string senderName)
    {
        var paragraphs = message.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
        return Build(subject, string.IsNullOrWhiteSpace(recipientName) ? "Hello," : $"Dear {recipientName},", paragraphs.Append($"Kind regards,\n{senderName}\niKamva").ToArray(), null, null);
    }

    private RenderedEmail Build(string subject, string greeting, string[] paragraphs, (string Label, string Url)? action, string? footnote)
    {
        static string H(string s) => WebUtility.HtmlEncode(s).Replace("\n", "<br>");

        var html = new StringBuilder();
        html.Append("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"><title>")
            .Append(H(subject)).Append("</title></head>")
            .Append("<body style=\"margin:0;padding:0;background:#efece5;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#1c1f20;\">")
            .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#efece5;padding:24px 12px;\"><tr><td align=\"center\">")
            .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:560px;background:#fcfbf8;border:1px solid #dcd7cc;border-radius:10px;overflow:hidden;\">")
            .Append("<tr><td style=\"background:#1d2021;padding:18px 28px;color:#f3f1ec;font-size:18px;font-weight:700;\">iKamva</td></tr>")
            .Append("<tr><td style=\"padding:28px;font-size:15px;line-height:1.6;\">")
            .Append("<p style=\"margin:0 0 14px;\">").Append(H(greeting)).Append("</p>");
        foreach (var p in paragraphs)
        {
            html.Append("<p style=\"margin:0 0 14px;\">").Append(H(p)).Append("</p>");
        }
        if (action is { } a)
        {
            html.Append("<p style=\"margin:22px 0;\"><a href=\"").Append(WebUtility.HtmlEncode(a.Url))
                .Append("\" style=\"display:inline-block;background:#3a5a6a;color:#ffffff;text-decoration:none;font-weight:600;padding:11px 20px;border-radius:8px;\">")
                .Append(H(a.Label)).Append("</a></p>")
                .Append("<p style=\"margin:0 0 14px;font-size:12px;color:#6e7374;\">If the button doesn’t work, copy this link into your browser:<br>")
                .Append(H(a.Url)).Append("</p>");
        }
        html.Append("</td></tr>");
        if (footnote is not null)
        {
            html.Append("<tr><td style=\"padding:16px 28px;border-top:1px solid #dcd7cc;font-size:12px;color:#6e7374;\">").Append(H(footnote)).Append("</td></tr>");
        }
        html.Append("</table></td></tr></table></body></html>");

        var text = new StringBuilder().AppendLine(greeting).AppendLine();
        foreach (var p in paragraphs)
        {
            text.AppendLine(p).AppendLine();
        }
        if (action is { } t)
        {
            text.AppendLine($"{t.Label}: {t.Url}").AppendLine();
        }
        if (footnote is not null)
        {
            text.AppendLine("--").AppendLine(footnote);
        }

        return new RenderedEmail(subject, html.ToString(), text.ToString().TrimEnd());
    }

    public static string Rand(decimal amount) => "R" + amount.ToString("#,##0.00", Money);

    private static string First(string fullName) => fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? fullName;

    private static string Duration(TimeSpan span) =>
        span.TotalHours >= 24 ? $"{(int)span.TotalDays} days" : span.TotalHours >= 1 ? $"{(int)span.TotalHours} hours" : $"{(int)span.TotalMinutes} minutes";
}
