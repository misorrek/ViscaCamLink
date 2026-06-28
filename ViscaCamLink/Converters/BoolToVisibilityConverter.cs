namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

[ValueConversion(typeof(bool), typeof(Visibility))]
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
   
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool)
        {
            return FallbackValue;
        }                
        return (bool)value ? TrueValue : FalseValue;
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
