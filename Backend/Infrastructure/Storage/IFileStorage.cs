namespace Backend.Infrastructure.Storage;

public sealed record StoredFile(Stream Content, string ContentType);

public interface IFileStorage
{
    Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct = default);

    Task<StoredFile?> OpenReadAsync(string key, CancellationToken ct = default);

    Task DeleteAsync(string key, CancellationToken ct = default);
}
