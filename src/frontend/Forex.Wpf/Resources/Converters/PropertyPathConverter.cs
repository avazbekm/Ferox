namespace Forex.Wpf.Resources.Converters;

using System.Globalization;
using System.Windows.Data;

public class PropertyPathConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] == null)
            return null;

        var obj = values[0];
        var propertyPath = values[1] as string;

        if (string.IsNullOrWhiteSpace(propertyPath))
            return null;

        try
        {
            var properties = propertyPath.Split('.');
            object? current = obj;

            foreach (var prop in properties)
            {
                if (current == null) return null;

                var propertyInfo = current.GetType().GetProperty(prop);
                if (propertyInfo == null) return null;

                current = propertyInfo.GetValue(current);
            }

            return current;
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
