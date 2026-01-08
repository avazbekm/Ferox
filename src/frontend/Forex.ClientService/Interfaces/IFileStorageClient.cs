namespace Forex.ClientService.Interfaces;

public interface IFileStorageClient
{
    Task<string?> UploadFileAsync(string filePath, CancellationToken ct = default);
    Task<string?> UploadFileAsync(Stream stream, string fileName, CancellationToken ct = default);
}
