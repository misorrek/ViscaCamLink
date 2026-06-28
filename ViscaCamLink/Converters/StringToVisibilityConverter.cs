namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(string), typeof(Visibility))]
public sealed class StringToVisibilityConverter : IValueConverter
{
    public StringToVisibilityConverter()
    {
        TrueValue = Visibility.Visible;
        FalseValue = Visibility.Hidden;
        FallbackValue = Visibility.Visible;
    }

    public Visibility TrueValue { get; set; }

    public Visibility FalseValue { get; set; }

    public Visibility FallbackValue { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string)
        {
            return FallbackValue;
        }
        return String.IsNullOrEmpty((string)value) ? FalseValue : TrueValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
