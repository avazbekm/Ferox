using System.Globalization;
using System.Windows.Data;

namespace Forex.Wpf.Resources.UserControls;

public class PropertyPathConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] == null || values[1] == null)
            return null;

        var item = values[0];
        var path = values[1].ToString();

        if (string.IsNullOrEmpty(path)) return item;

        try
        {
            foreach (var part in path.Split('.'))
            {
                if (item == null) return null;
                var prop = item.GetType().GetProperty(part);
                if (prop == null) return null;
                item = prop.GetValue(item);
            }
            return item;
        }
        catch
        {
            return null;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
