namespace Forex.Wpf.Resources.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

public class NumberToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null) return false;

        try
        {
            double number = System.Convert.ToDouble(value);

            if (parameter?.ToString()?.ToLower() == "inverse")
                return number <= 0;

            return number > 0;
        }
        catch
        {
            return false;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
