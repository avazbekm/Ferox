namespace Forex.ClientService.Models.Responses;

public record PresignedUrlResponse
{
    public string Url { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
