using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Storage;

public class SupabaseFileStorage : IFileStorage
{
    private readonly HttpClient _http;
    private readonly StorageOptions _options;

    public SupabaseFileStorage(HttpClient http, IOptions<StorageOptions> options)
    {
        _options = options.Value;
        _http = http;
        _http.BaseAddress = new Uri(_options.SupabaseUrl!.TrimEnd('/') + "/storage/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.SupabaseServiceKey);
        _http.DefaultRequestHeaders.Add("apikey", _options.SupabaseServiceKey);
    }

    public async Task SaveAsync(string key, Stream content, string contentType, CancellationToken ct = default)
    {
        using var body = new StreamContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using var request = new HttpRequestMessage(HttpMethod.Post, ObjectPath(key)) { Content = body };
        request.Headers.Add("x-upsert", "true");
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<StoredFile?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(ObjectPath(key), HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
        {
            response.Dispose();
            return null;
        }

        response.EnsureSuccessStatusCode();
        var type = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return new StoredFile(await response.Content.ReadAsStreamAsync(ct), type);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        using var response = await _http.DeleteAsync(ObjectPath(key), ct);
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    private string ObjectPath(string key) =>
        $"object/{Uri.EscapeDataString(_options.Bucket)}/{string.Join('/', key.Split('/').Select(Uri.EscapeDataString))}";
}
