namespace Forex.Wpf.Services;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.IO;

public static class ImageCompressionService
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024;
    private const int MaxDimension = 2048;
    private const int JpegQuality = 85;

    public static async Task<Stream> CompressImageIfNeededAsync(Stream inputStream, string fileName)
    {
        inputStream.Position = 0;
        
        if (inputStream.Length <= MaxFileSizeBytes)
        {
            inputStream.Position = 0;
            return inputStream;
        }

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
        outputStream.Position = 0;
        
        return outputStream;
    }

    public static string EnsureJpegExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is ".jpg" or ".jpeg")
            return fileName;

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        return $"{nameWithoutExtension}.jpg";
    }
}
