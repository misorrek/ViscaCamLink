namespace ViscaCamLink.Converters;

using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

[ValueConversion(typeof(bool[]), typeof(bool))]
public class BoolAndConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.Cast<bool>().All(value => value);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
