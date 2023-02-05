namespace ViscaCamLink.Util;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

[ValueConversion(typeof(Boolean), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public BoolToVisibilityConverter()
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
        if (value is not Boolean)
        {
            return FallbackValue;
        }                
        return (Boolean)value ? TrueValue : FalseValue;
    }

    public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
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
