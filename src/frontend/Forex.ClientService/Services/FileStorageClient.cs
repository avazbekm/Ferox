namespace Forex.ClientService.Services;

using Forex.ClientService.Interfaces;
using Forex.ClientService.Models.Requests;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

public class FileStorageClient(IApiProductEntries productEntriesApi, IHttpClientFactory httpClientFactory) : IFileStorageClient
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public async Task<string?> UploadFileAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath)) return null;

        var fileName = Path.GetFileName(filePath);
        var contentType = GetContentType(fileName);

        using var stream = File.OpenRead(filePath);
        return await UploadFileAsync(stream, fileName, contentType, ct);
    }

    public async Task<string?> UploadFileAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        return await UploadFileAsync(stream, fileName, "application/octet-stream", ct);
    }

    private async Task<string?> UploadFileAsync(Stream stream, string fileName, string contentType, CancellationToken ct)
    {
        try
        {
            var response = await productEntriesApi.GenerateUploadUrl(new GenerateUploadUrlRequest
            {
                FileName = fileName
            });

            if (response.IsSuccess && response.Data != null)
            {
                using var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

                var uploadResponse = await _httpClient.PutAsync(response.Data.Url, content, ct);

                if (uploadResponse.IsSuccessStatusCode)
                    return response.Data.Key;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetContentType(string fileName)
        => Path.GetExtension(fileName)
        .ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream",
        };
}
