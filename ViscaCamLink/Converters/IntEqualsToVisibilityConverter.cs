namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(int), typeof(Visibility))]
public class IntEqualsToVisibilityConverter : IMultiValueConverter
{
    public Visibility TrueValue { get; set; } = Visibility.Collapsed;

    public Visibility FalseValue { get; set; } = Visibility.Visible;

    public Visibility FallbackValue { get; set; } = Visibility.Visible;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is int first && values[1] is int second)
        {
            return first == second ? TrueValue : FalseValue;
        }

        return FallbackValue;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
