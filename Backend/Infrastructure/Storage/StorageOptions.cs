namespace Backend.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string LocalPath { get; set; } = "App_Data/uploads";

    public string? SupabaseUrl { get; set; }

    public string? SupabaseServiceKey { get; set; }

    public string Bucket { get; set; } = "verification-documents";

    public bool UseSupabase => !string.IsNullOrWhiteSpace(SupabaseUrl) && !string.IsNullOrWhiteSpace(SupabaseServiceKey);
}
