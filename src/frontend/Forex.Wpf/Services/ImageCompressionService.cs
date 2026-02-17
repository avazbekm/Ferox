namespace Forex.Wpf.Services;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System.IO;

public static class ImageCompressionService
{
    private const int MaxFileSizeBytes = 512 * 1024; // 512 KB
    private const int MaxDimension = 1920;
    private const int MaxQuality = 95;
    private const int MinQuality = 40;

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

        var outputStream = await FindOptimalQualityAsync(image);

        if (outputStream.Length > MaxFileSizeBytes)
        {
            outputStream.Dispose();
            outputStream = await CompressWithResizingAsync(image);
        }

        outputStream.Position = 0;
        return outputStream;
    }

    private static async Task<MemoryStream> FindOptimalQualityAsync(Image image)
    {
        int minQuality = MinQuality;
        int maxQuality = MaxQuality;
        MemoryStream? bestStream = null;

        while (minQuality <= maxQuality)
        {
            int midQuality = (minQuality + maxQuality) / 2;
            var testStream = new MemoryStream();

            var encoder = new WebpEncoder 
            { 
                Quality = midQuality,
                FileFormat = WebpFileFormatType.Lossy,
                Method = WebpEncodingMethod.BestQuality
            };

            await image.SaveAsync(testStream, encoder);

            if (testStream.Length <= MaxFileSizeBytes)
            {
                bestStream?.Dispose();
                bestStream = testStream;
                
                minQuality = midQuality + 1;
            }
            else
            {
                testStream.Dispose();
                maxQuality = midQuality - 1;
            }
        }

        if (bestStream == null)
        {
            bestStream = new MemoryStream();
            var encoder = new WebpEncoder 
            { 
                Quality = MinQuality,
                FileFormat = WebpFileFormatType.Lossy 
            };
            await image.SaveAsync(bestStream, encoder);
        }

        return bestStream;
    }

    private static async Task<MemoryStream> CompressWithResizingAsync(Image image)
    {
        var scaleFactor = 0.85;
        var outputStream = new MemoryStream();

        while (outputStream.Length > MaxFileSizeBytes && image.Width > 200 && image.Height > 200)
        {
            var newWidth = (int)(image.Width * scaleFactor);
            var newHeight = (int)(image.Height * scaleFactor);

            image.Mutate(x => x.Resize(newWidth, newHeight));

            outputStream.SetLength(0);
            outputStream.Position = 0;

            var encoder = new WebpEncoder 
            { 
                Quality = MinQuality,
                FileFormat = WebpFileFormatType.Lossy 
            };
            
            await image.SaveAsync(outputStream, encoder);

            if (outputStream.Length <= MaxFileSizeBytes)
                break;
        }

        return outputStream;
    }
}
