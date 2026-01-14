namespace Forex.Wpf.Resources.Converters;

using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class UniversalImageResolver : IMultiValueConverter
{
    private static readonly ImageSource? placeholder = LoadPlaceholder;

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // Mudofaa: qiymatlar yetarli bo'lmasa darhol placeholder qaytaramiz
        if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
            return placeholder;

        var contextItem = values[0];
        var pathPropertyName = values[1].ToString();

        try
        {
            // Reflection orqali qiymatni olish
            var prop = contextItem.GetType().GetProperty(pathPropertyName);
            var rawValue = prop?.GetValue(contextItem);

            if (rawValue == null) return placeholder;

            // Agar matn so'ralayotgan bo'lsa (TextBlock uchun)
            if (targetType == typeof(string))
                return rawValue.ToString();

            string rawPath = rawValue.ToString()!;
            if (string.IsNullOrWhiteSpace(rawPath)) return placeholder;

            // MUHIM: URL bo'lsa yangi BitmapImage yaratish mantiqi
            var bitmap = new BitmapImage();
            bitmap.BeginInit();

            if (Uri.TryCreate(rawPath, UriKind.Absolute, out Uri? uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                bitmap.UriSource = uri;
                // Internet rasmlari uchun keshni biroz o'zgartiramiz
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
            }
            else if (File.Exists(rawPath))
            {
                bitmap.UriSource = new Uri(rawPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
            }
            else return placeholder;

            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.EndInit();

            // Agar rasm hali yuklanayotgan bo'lsa (Internetdan bo'lsa), Freeze() ba'zan xato beradi
            if (!bitmap.IsDownloading)
            {
                bitmap.Freeze();
            }

            return bitmap;
        }
        catch
        {
            return placeholder;
        }
    }

    private static ImageSource? LoadPlaceholder
    {
        get
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
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException("Faqat bir tomonga konvertatsiya qilish mumkin.");
}