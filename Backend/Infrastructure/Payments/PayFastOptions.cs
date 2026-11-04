namespace Backend.Infrastructure.Payments;

public sealed class PayFastOptions
{
    public const string SectionName = "PayFast";

    public string MerchantId { get; set; } = string.Empty;

    public string MerchantKey { get; set; } = string.Empty;

    public string? Passphrase { get; set; }

    public bool UseSandbox { get; set; } = true;

    public string PublicApiUrl { get; set; } = string.Empty;

    public string ClientUrl { get; set; } = "http://localhost:4200";

    public string? ReturnUrl { get; set; }

    public string? CancelUrl { get; set; }

    public string? NotifyUrl { get; set; }

    public bool VerifySourceIp { get; set; } = true;

    public string Host => UseSandbox ? "sandbox.payfast.co.za" : "www.payfast.co.za";

    public string ProcessUrl => $"https://{Host}/eng/process";

    public string ValidateUrl => $"https://{Host}/eng/query/validate";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(MerchantId) && !string.IsNullOrWhiteSpace(MerchantKey) && !string.IsNullOrWhiteSpace(PublicApiUrl);
}
