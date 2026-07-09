namespace ViscaCamLink.Converters;

using System.Globalization;
using System.Windows;
using System.Windows.Data;

[ValueConversion(typeof(int), typeof(Visibility))]
public class IntEqualsToVisibilityConverter : IMultiValueConverter
{
    public IntEqualsToVisibilityConverter()
    {
        TrueValue = Visibility.Collapsed;
        FalseValue = Visibility.Visible;
        FallbackValue = Visibility.Visible;
    }

    public Visibility TrueValue { get; set; }

    public Visibility FalseValue { get; set; }

    public Visibility FallbackValue { get; set; }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is int a && values[1] is int b)
        {
            return a == b ? TrueValue : FalseValue;
        }

        return FallbackValue;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
