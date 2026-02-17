namespace Forex.Application.Common.Options;

public sealed class FileUploadOptions
{
    public int MaxFileSizeMB { get; init; } = 5;
    public long MaxFileSizeBytes => MaxFileSizeMB * 1024 * 1024;

    public string[] AllowedImageExtensions { get; init; } = [".jpg", ".jpeg", ".png", ".webp"];
    public string[] AllowedMimeTypes { get; init; } = ["image/jpeg", "image/png", "image/webp"];
}
