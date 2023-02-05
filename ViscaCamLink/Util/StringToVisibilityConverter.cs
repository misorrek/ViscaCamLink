namespace ViscaCamLink.Util;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(String), typeof(Visibility))]
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

    public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        if (value is not String)
        {
            return FallbackValue;
        }
        return String.IsNullOrEmpty((String)value) ? FalseValue : TrueValue;
    }

    public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
