namespace Forex.Wpf.Resources.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

public class NumberToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null) return Visibility.Collapsed;

        try
        {
            double number = System.Convert.ToDouble(value);
            bool isPositive = number > 0;

            bool result = parameter?.ToString()!.ToLower() == "inverse" ? !isPositive : isPositive;

            return result ? Visibility.Visible : Visibility.Collapsed;
        }
        catch
        {
            return Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}