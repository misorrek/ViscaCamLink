namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    public Visibility TrueValue { get; set; } = Visibility.Visible;

    public Visibility FalseValue { get; set; } = Visibility.Hidden;

    public Visibility FallbackValue { get; set; } = Visibility.Visible;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
        {
            return FallbackValue;
        }

        return boolValue ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (Equals(value, TrueValue))
        {
            return true;
        }

        if (Equals(value, FalseValue))
        {
            return false;
        }

        return FallbackValue;
    }
}
