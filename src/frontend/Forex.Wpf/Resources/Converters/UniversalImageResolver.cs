namespace Forex.Wpf.Resources.Converters;

using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class UniversalImageResolver : IMultiValueConverter
{
    private static readonly Lazy<ImageSource?> _placeholder = new(LoadPlaceholder);

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is null || values.Length < 2 || values[0] is null || values[1] is null)
            return _placeholder.Value;

        var contextItem = values[0];
        var pathPropertyName = values[1].ToString();

        try
        {
            var prop = contextItem.GetType().GetProperty(pathPropertyName);
            var rawValue = prop?.GetValue(contextItem);

            if (rawValue is null)
                return _placeholder.Value;

            if (targetType == typeof(string))
                return rawValue.ToString();

            string rawPath = rawValue.ToString()!;
            if (string.IsNullOrWhiteSpace(rawPath))
                return _placeholder.Value;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

            if (Uri.TryCreate(rawPath, UriKind.Absolute, out Uri? uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                bitmap.UriSource = uri;
            else if (File.Exists(rawPath))
                bitmap.UriSource = new Uri(rawPath);
            else
                return _placeholder.Value;

            bitmap.EndInit();

            if (!bitmap.IsDownloading)
                bitmap.Freeze();

            return bitmap;
        }
        catch
        {
            return _placeholder.Value;
        }
    }

    private static ImageSource? LoadPlaceholder()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Resources/Assets/default.png");
            var bitmap = new BitmapImage(uri);
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}