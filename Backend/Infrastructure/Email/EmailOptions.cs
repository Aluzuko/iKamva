namespace Backend.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = "no-reply@ikamva.local";

    public string FromName { get; set; } = "iKamva";

    public string? ReplyToAddress { get; set; }

    public string ClientUrl { get; set; } = string.Empty;

    public string Transport { get; set; } = "Pickup";

    public string PickupDirectory { get; set; } = "App_Data/mail-pickup";

    public SmtpSettings Smtp { get; set; } = new();

    public WorkerSettings Worker { get; set; } = new();

    public int MaxAttachmentBytes { get; set; } = 10 * 1024 * 1024;

    public int MaxAttachments { get; set; } = 10;

    public sealed class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool EnableSsl { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
    }

    public sealed class WorkerSettings
    {
        public bool Enabled { get; set; }
        public int IntervalSeconds { get; set; } = 30;
        public int BatchSize { get; set; } = 25;
    }
}
