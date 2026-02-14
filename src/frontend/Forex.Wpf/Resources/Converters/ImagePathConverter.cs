namespace Forex.Wpf.Resources.Converters;

using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class ImagePathConverter : IValueConverter
{
    private static ImageSource? _placeholder;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return GetPlaceholder();

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

            if (Uri.TryCreate(path, UriKind.Absolute, out Uri? uri))
            {
                bitmap.UriSource = uri;
            }
            else if (File.Exists(path))
            {
                bitmap.UriSource = new Uri(Path.GetFullPath(path));
            }
            else
            {
                return GetPlaceholder();
            }

            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return GetPlaceholder();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private ImageSource GetPlaceholder()
    {
        if (_placeholder == null)
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Forex.Wpf;component/Resources/Assets/default.png");
                _placeholder = new BitmapImage(uri);
                _placeholder.Freeze();
            }
            catch
            {
                // Fallback to empty image if resource not found
                var bitmap = new BitmapImage();
                bitmap.Freeze();
                _placeholder = bitmap;
            }
        }
        return _placeholder;
    }
}
