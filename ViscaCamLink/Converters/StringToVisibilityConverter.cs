namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(string), typeof(Visibility))]
public class StringToVisibilityConverter : IValueConverter
{
    public Visibility TrueValue { get; set; } = Visibility.Visible;

    public Visibility FalseValue { get; set; } = Visibility.Hidden;

    public Visibility FallbackValue { get; set; } = Visibility.Visible;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string stringValue)
        {
            return FallbackValue;
        }

        return string.IsNullOrEmpty(stringValue) ? FalseValue : TrueValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
