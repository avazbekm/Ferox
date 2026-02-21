namespace Forex.Infrastructure.Storage;

public sealed class MinioStorageOptions
{
    /// <summary>
    /// Internal MinIO endpoint (e.g., "minio:9000" for Docker network)
    /// </summary>
    public string Endpoint { get; init; } = default!;

    /// <summary>
    /// Public MinIO endpoint accessible from clients. 
    /// If not set, will be auto-detected from HTTP request Host header.
    /// Format: "host:port" (e.g., "localhost:9000" or "myserver.com:9000")
    /// </summary>
    public string? PublicEndpoint { get; init; }

    /// <summary>
    /// MinIO port for public access. Defaults to 9000 if not specified.
    /// Used for auto-detection when PublicEndpoint is not configured.
    /// </summary>
    public int PublicPort { get; init; } = 9000;

    public string AccessKey { get; init; } = default!;
    public string SecretKey { get; init; } = default!;
    public string BucketName { get; init; } = default!;
    public bool UseSsl { get; init; }
    public bool EnablePublicRead { get; init; }
    public string Prefix { get; init; } = "uploads";
}
