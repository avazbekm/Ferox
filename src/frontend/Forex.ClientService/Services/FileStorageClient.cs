namespace Forex.ClientService.Services;

using Forex.ClientService.Interfaces;
using Forex.ClientService.Models.Requests;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

public class FileStorageClient(IApiProductEntries productEntriesApi, IHttpClientFactory httpClientFactory, IConfiguration configuration) : IFileStorageClient
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

            if (response.IsSuccess)
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

    public string GetFullUrl(string? objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey)) return string.Empty;

        if (objectKey.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return objectKey;

        var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
        var minioEndpoint = configuration["MinIO:PublicEndpoint"] ?? "localhost:9000";
        var bucketName = configuration["MinIO:BucketName"] ?? "forex-uploads";

        return $"http://{minioEndpoint}/{bucketName}/{objectKey.TrimStart('/')}";
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
