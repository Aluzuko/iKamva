using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options, IWebHostEnvironment env)
    {
        _root = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.Value.LocalPath));
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
        await File.WriteAllTextAsync(path + ".type", contentType, ct);
    }

    public async Task<StoredFile?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);
        if (!File.Exists(path))
        {
            return null;
        }

        var type = File.Exists(path + ".type") ? await File.ReadAllTextAsync(path + ".type", ct) : "application/octet-stream";
        return new StoredFile(File.OpenRead(path), type);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var path = Resolve(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        if (File.Exists(path + ".type"))
        {
            File.Delete(path + ".type");
        }
        return Task.CompletedTask;
    }

    private string Resolve(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage key.");
        }
        return path;
    }
}
