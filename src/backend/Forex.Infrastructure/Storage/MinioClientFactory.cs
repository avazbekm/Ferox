namespace Forex.Infrastructure.Storage;

using Microsoft.Extensions.Options;
using Minio;

/// <summary>
/// Factory for creating MinIO clients with proper endpoint configuration
/// </summary>
public class ForexMinioClientFactory
{
    private readonly MinioStorageOptions _options;

    public ForexMinioClientFactory(IOptions<MinioStorageOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Creates MinIO client for internal operations (bucket management, file operations)
    /// Uses internal Docker network endpoint
    /// </summary>
    public IMinioClient CreateInternalClient()
    {
        var uri = new Uri(_options.Endpoint.StartsWith("http")
            ? _options.Endpoint
            : $"http://{_options.Endpoint}");

        var builder = new MinioClient()
            .WithEndpoint(uri.Authority)
            .WithCredentials(_options.AccessKey, _options.SecretKey);

        if (_options.UseSsl || uri.Scheme == Uri.UriSchemeHttps)
            builder.WithSSL();

        return builder.Build();
    }

    /// <summary>
    /// Creates MinIO client for presigned URL generation
    /// Uses public endpoint if specified, otherwise internal endpoint
    /// </summary>
    public IMinioClient CreatePublicClient(string? requestHost = null)
    {
        var publicEndpoint = DeterminePublicEndpoint(requestHost);

        var uri = new Uri(publicEndpoint.StartsWith("http")
            ? publicEndpoint
            : $"http://{publicEndpoint}");

        var builder = new MinioClient()
            .WithEndpoint(uri.Authority)
            .WithCredentials(_options.AccessKey, _options.SecretKey);

        if (_options.UseSsl || uri.Scheme == Uri.UriSchemeHttps)
            builder.WithSSL();

        return builder.Build();
    }

    public string DeterminePublicEndpoint(string? requestHost)
    {
        // Priority 1: Explicit configuration
        if (!string.IsNullOrWhiteSpace(_options.PublicEndpoint))
        {
            Console.WriteLine($"[MinIO] Using PublicEndpoint from config: {_options.PublicEndpoint}");
            return _options.PublicEndpoint;
        }

        // Priority 2: Auto-detect from HTTP request host
        if (!string.IsNullOrWhiteSpace(requestHost))
        {
            var hostParts = requestHost.Split(':');
            var hostname = hostParts[0];
            var result = $"{hostname}:{_options.PublicPort}";
            Console.WriteLine($"[MinIO] Auto-detected from request: {result}");
            return result;
        }

        // Priority 3: Fallback to internal endpoint
        Console.WriteLine($"[MinIO] WARNING: Using internal endpoint as fallback: {_options.Endpoint}");
        return _options.Endpoint;
    }
}
