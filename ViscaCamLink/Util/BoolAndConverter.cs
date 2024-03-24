namespace ViscaCamLink.Util;

using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

public class BoolAndConverter : IMultiValueConverter
{
    public Object Convert(Object[] values, Type targetType, Object parameter, CultureInfo culture)
    {
        return values.Cast<Boolean>().All(value => value);
    }

    public Object[] ConvertBack(Object value, Type[] targetTypes, Object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
