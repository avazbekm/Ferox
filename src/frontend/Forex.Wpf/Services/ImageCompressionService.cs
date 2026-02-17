namespace Forex.Wpf.Services;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.IO;

public static class ImageCompressionService
{
    private const int MaxFileSizeBytes = 512 * 1024;
    private const int MaxDimension = 1920;
    private const int JpegQuality = 82;

    public static async Task<Stream> CompressImageAsync(Stream inputStream)
    {
        inputStream.Position = 0;

        using var image = await Image.LoadAsync(inputStream);

        if (image.Width > MaxDimension || image.Height > MaxDimension)
        {
            var ratio = Math.Min((double)MaxDimension / image.Width, (double)MaxDimension / image.Height);
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            image.Mutate(x => x.Resize(newWidth, newHeight));
        }

        var outputStream = new MemoryStream();
        var encoder = new JpegEncoder { Quality = JpegQuality };

        await image.SaveAsync(outputStream, encoder);

        if (outputStream.Length > MaxFileSizeBytes)
        {
            outputStream.Dispose();
            outputStream = new MemoryStream();

            var lowerQualityEncoder = new JpegEncoder { Quality = 70 };
            await image.SaveAsync(outputStream, lowerQualityEncoder);
        }

        outputStream.Position = 0;
        return outputStream;
    }
}
